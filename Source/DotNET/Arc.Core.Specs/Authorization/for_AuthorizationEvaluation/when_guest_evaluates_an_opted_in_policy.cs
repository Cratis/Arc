// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_guest_evaluates_an_opted_in_policy : Specification
{
    bool _allowed;
    bool _denied;
    bool _defaultDenied;
    bool _roleDenied;
    bool _stackedDenied;
    bool _guestWasEmpty;
    bool _sameDeclaration;
    bool _differentDeclaration;
    bool _methodReplacesRoles;
    bool _typeStillRequiresRoles;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns((ClaimsPrincipal?)null);
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var evaluator = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var runtime = new ArcAuthorizationPolicyRuntime(
        [
            new AuthorizationPolicyRegistration("Guest", typeof(GuestPolicy)) { EvaluatesAnonymous = true },
            new AuthorizationPolicyRegistration("Default", typeof(GuestPolicy))
        ]);
        await using var services = new ServiceCollection().AddSingleton<GuestPolicy>().BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(declarations, evaluator, accessor, runtime);
        _allowed = await evaluation.IsAuthorized(typeof(GuestCommand), new object(), services, CancellationToken.None);
        _guestWasEmpty = GuestPolicy.WasEmpty;
        _denied = await evaluation.IsAuthorized(typeof(RejectedCommand), new object(), services, CancellationToken.None);
        _defaultDenied = await evaluation.IsAuthorized(typeof(DefaultCommand), new object(), services, CancellationToken.None);
        _roleDenied = await evaluation.IsAuthorized(typeof(RoleCommand), new object(), services, CancellationToken.None);
        _stackedDenied = await evaluation.IsAuthorized(typeof(StackedCommand), new object(), services, CancellationToken.None);
        _methodReplacesRoles = await evaluation.IsAuthorized(typeof(RestrictedReadModel).GetMethod(nameof(RestrictedReadModel.Public))!, new object(), services, CancellationToken.None);
        _typeStillRequiresRoles = await evaluation.IsAuthorized(typeof(RestrictedReadModel), new object(), services, CancellationToken.None);
        _sameDeclaration = AuthorizationEvaluator.SameDeclaration(declarations.For(typeof(GuestCommand)), declarations.For(typeof(GuestCommand)));
        _differentDeclaration = AuthorizationEvaluator.SameDeclaration(declarations.For(typeof(GuestCommand)), declarations.For(typeof(RoleCommand)));
    }

    [Fact] void should_authorize_an_opted_in_guest() => _allowed.ShouldBeTrue();
    [Fact] void should_pass_an_empty_unauthenticated_principal() => _guestWasEmpty.ShouldBeTrue();
    [Fact] void should_obey_the_policy_rejection() => _denied.ShouldBeFalse();
    [Fact] void should_require_authentication_without_opt_in() => _defaultDenied.ShouldBeFalse();
    [Fact] void should_require_authentication_for_roles() => _roleDenied.ShouldBeFalse();
    [Fact] void should_require_every_stacked_requirement_to_opt_in() => _stackedDenied.ShouldBeFalse();
    [Fact] void should_allow_an_opted_in_method_replacing_type_roles() => _methodReplacesRoles.ShouldBeTrue();
    [Fact] void should_still_deny_the_type_level_roles() => _typeStillRequiresRoles.ShouldBeFalse();
    [Fact] void should_compare_the_evaluated_declaration() => _sameDeclaration.ShouldBeTrue();
    [Fact] void should_not_reuse_the_guest_verdict_for_a_role_declaration() => _differentDeclaration.ShouldBeFalse();

    [Authorize(Policy = "Guest")]
    public record GuestCommand;

    [Authorize(Policy = "Guest")]
    public record RejectedCommand;

    [Authorize(Policy = "Default")]
    public record DefaultCommand;

    [Authorize(Policy = "Guest", Roles = "Admin")]
    public record RoleCommand;

    [Authorize(Policy = "Guest")]
    [Authorize]
    public record StackedCommand;

    [Roles("Admin")]
    public static class RestrictedReadModel
    {
        [Authorize(Policy = "Guest")]
        public static void Public() { }
    }

    public class GuestPolicy : IAuthorizationPolicy
    {
        public static bool WasEmpty { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            WasEmpty = context.Principal.Identity?.IsAuthenticated == false && !context.Principal.Claims.Any();
            return ValueTask.FromResult(context.Target != typeof(RejectedCommand));
        }
    }
}
