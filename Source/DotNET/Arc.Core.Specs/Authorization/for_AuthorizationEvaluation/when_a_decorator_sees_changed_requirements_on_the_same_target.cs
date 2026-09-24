// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_decorator_sees_changed_requirements_on_the_same_target : Specification
{
    Exception? _failure;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => Array.Empty<IAnonymousEvaluator>().AsEnumerable().GetEnumerator());
        var changing = new ChangingRequirements();
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { changing }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test")));
        var builtIn = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var decorator = new DelegatingEvaluator(builtIn);
        await using var services = new ServiceCollection().AddSingleton<AllowingPolicy>().BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            decorator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allowed", typeof(AllowingPolicy))]));
        _failure = await Catch.Exception(() => evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None));
    }

    [Fact] void should_not_extend_a_completed_verdict_to_different_requirements() => _failure.ShouldBeOfExactType<AsynchronousAuthorizationRequired>();

    public record ProtectedCommand;

    class ChangingRequirements : IAuthorizationAttributeEvaluator
    {
        int _reads;

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => (true, "Admin");

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type)
        {
            var policy = Interlocked.Increment(ref _reads) <= 2 ? "Allowed" : "Changed";
            return [AuthorizationRequirement.FromAttribute("Admin", policy, null)];
        }
    }

    class DelegatingEvaluator(AuthorizationEvaluator inner) : IAuthorizationEvaluator
    {
        public bool IsAuthorized(Type type) => inner.IsAuthorized(type);

        public bool IsAuthorized(MethodInfo method) => inner.IsAuthorized(method);
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
