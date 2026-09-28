// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_policy_mutates_shared_requirement_lists : Specification
{
    bool _schemeAuthorized;
    bool _roleAuthorized;
    bool _schemeLegacyInvoked;
    bool _roleLegacyInvoked;

    async Task Because()
    {
        var schemes = new List<string> { "Bearer" };
        _schemeAuthorized = await Authorize(new List<string>(), schemes, () => schemes[0] = "Changed", () => _schemeLegacyInvoked = true);

        var roles = new List<string> { "Admin" };
        _roleAuthorized = await Authorize(roles, new List<string>(), () => roles[0] = "Other", () => _roleLegacyInvoked = true);
    }

    [Fact] void should_deny_when_the_policy_mutates_shared_schemes() => _schemeAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_legacy_evaluator_after_schemes_change() => _schemeLegacyInvoked.ShouldBeFalse();
    [Fact] void should_deny_when_the_policy_mutates_shared_roles() => _roleAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_legacy_evaluator_after_roles_change() => _roleLegacyInvoked.ShouldBeFalse();

    static async Task<bool> Authorize(List<string> roles, List<string> schemes, Action mutate, Action onLegacyVerdict)
    {
        var requirement = new AuthorizationRequirement(roles) { Policy = "Allowed", AuthenticationSchemes = schemes };
        var attributes = new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new SharedRequirement(requirement)]);
        var anonymous = new KnownInstancesOf<IAnonymousEvaluator>([]);
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test")));
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(ProtectedCommand)).Returns(_ =>
        {
            onLegacyVerdict();
            return true;
        });
        var runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resolution));
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<ClaimsPrincipal?>(0)));
        resolution.IsAuthorized(Arg.Any<AuthorizationPolicyContext>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                mutate();
                return Task.FromResult(true);
            });
        await using var services = new ServiceCollection().BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(new AuthorizationDeclarations(anonymous, attributes), evaluator, accessor, runtime);
        return await evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None);
    }

    public record ProtectedCommand;

    class SharedRequirement(AuthorizationRequirement requirement) : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => type == typeof(ProtectedCommand) ? [requirement] : [];
    }
}
