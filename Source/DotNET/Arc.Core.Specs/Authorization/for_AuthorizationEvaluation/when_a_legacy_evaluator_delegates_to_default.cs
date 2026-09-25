// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_legacy_evaluator_delegates_to_default : Specification
{
    bool _allowed;
    Exception? _differentTargetFailure;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "caller"), new Claim(ClaimTypes.Role, "Admin")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        var builtIn = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var decorator = new DelegatingEvaluator(builtIn, error => _differentTargetFailure = error);
        await using var services = new ServiceCollection()
            .AddSingleton<AllowingPolicy>()
            .BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            decorator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allowed", typeof(AllowingPolicy))]));
        _allowed = await evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None);
    }

    [Fact] void should_allow_a_decorator_that_checks_the_same_effective_roles_and_policy() => _allowed.ShouldBeTrue();
    [Fact] void should_not_reuse_the_marker_for_a_different_target() => _differentTargetFailure.ShouldBeOfExactType<AsynchronousAuthorizationRequired>();

    [Authorize(Policy = "Allowed", Roles = "Admin")]
    public record ProtectedCommand;

    [Authorize(Policy = "Denied")]
    public record OtherCommand;

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    class DelegatingEvaluator(AuthorizationEvaluator inner, Action<Exception?> observeDifferentTarget) : IAuthorizationEvaluator
    {
        public bool IsAuthorized(Type type)
        {
            var allowed = inner.IsAuthorized(type);
            observeDifferentTarget(Catch.Exception(() => inner.IsAuthorized(typeof(OtherCommand))));
            return allowed;
        }

        public bool IsAuthorized(MethodInfo method) => inner.IsAuthorized(method);
    }
}
