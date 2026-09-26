// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_evaluating_guest_policies_from_both_attribute_families : given.both_attribute_families
{
    bool _arc;
    bool _aspNet;
    bool _default;
    bool _scheme;
    bool _authenticatedPolicy;
    bool _rejected;
    bool _equivalentDeclarations;
    bool _stacked;
    bool _nativeOptedIn;
    bool _aspNetOptedIn;

    async Task Because()
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddAuthorizationBuilder()
            .AddPolicy("Public", policy => policy.RequireAssertion(_ => true))
            .AddPolicy("Private", policy => policy.RequireAssertion(_ => false))
            .AddPolicy("Default", policy => policy.RequireAssertion(_ => true))
            .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser().RequireAssertion(_ => true));
        registrations.AddArcAnonymousAspNetAuthorizationPolicy("public");
        registrations.AddArcAnonymousAspNetAuthorizationPolicy("Private");
        registrations.AddArcAnonymousAspNetAuthorizationPolicy("Authenticated");
        registrations.AddArcAuthorizationPolicy<GuestNativePolicy>("NativeOpted", evaluatesAnonymous: true);
        registrations.AddArcAuthorizationPolicy<GuestNativePolicy>("NativeDefault");
        registrations.AddAuthorizationBuilder().AddPolicy("AspNetDefault", policy => policy.RequireAssertion(_ => true));
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Missing").Returns(Task.FromResult<AuthenticationScheme?>(
            new AuthenticationScheme("Missing", "Missing", typeof(IAuthenticationHandler))));
        registrations.AddSingleton(schemes);
        await using var services = registrations.BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(
            new ArcAuthorizationPolicyRuntime(services.GetServices<AuthorizationPolicyRegistration>()),
            services.GetServices<AnonymousAspNetAuthorizationPolicyRegistration>());
        var evaluation = new AuthorizationEvaluation(ArcDeclarationsFirst(), ArcFirst(), _currentPrincipalAccessor, runtime);
        _arc = await evaluation.IsAuthorized(typeof(ArcPublic), new object(), services, CancellationToken.None);
        _aspNet = await evaluation.IsAuthorized(typeof(AspNetPublic), new object(), services, CancellationToken.None);
        _default = await evaluation.IsAuthorized(typeof(AspNetDefault), new object(), services, CancellationToken.None);
        _scheme = await evaluation.IsAuthorized(typeof(AspNetScheme), new object(), services, CancellationToken.None);
        _authenticatedPolicy = await evaluation.IsAuthorized(typeof(AspNetAuthenticated), new object(), services, CancellationToken.None);
        _rejected = await evaluation.IsAuthorized(typeof(AspNetPrivate), new object(), services, CancellationToken.None);
        _stacked = await evaluation.IsAuthorized(typeof(Stacked), new object(), services, CancellationToken.None);
        _nativeOptedIn = await evaluation.IsAuthorized(typeof(NativeOptedAspNetDefault), new object(), services, CancellationToken.None);
        _aspNetOptedIn = await evaluation.IsAuthorized(typeof(AspNetOptedNativeDefault), new object(), services, CancellationToken.None);
        _equivalentDeclarations = AuthorizationEvaluator.SameDeclaration(
            ArcDeclarationsFirst().For(typeof(ArcPublic)), AspNetDeclarationsFirst().For(typeof(AspNetPublic)));
    }

    [Fact] void should_allow_arc_attribute_guest() => _arc.ShouldBeTrue();
    [Fact] void should_allow_aspnet_attribute_guest() => _aspNet.ShouldBeTrue();
    [Fact] void should_require_explicit_opt_in() => _default.ShouldBeFalse();
    [Fact] void should_require_scheme_authentication() => _scheme.ShouldBeFalse();
    [Fact] void should_require_authentication_when_policy_requires_it() => _authenticatedPolicy.ShouldBeFalse();
    [Fact] void should_obey_policy_rejection() => _rejected.ShouldBeFalse();
    [Fact] void should_reject_a_stack_with_only_one_opt_in() => _stacked.ShouldBeFalse();
    [Fact] void should_reject_a_native_opt_in_with_an_aspnet_default() => _nativeOptedIn.ShouldBeFalse();
    [Fact] void should_reject_an_aspnet_opt_in_with_a_native_default() => _aspNetOptedIn.ShouldBeFalse();
    [Fact] void should_treat_both_attribute_families_as_the_same_declaration() => _equivalentDeclarations.ShouldBeTrue();

    [Cratis.Arc.Authorization.Authorize(Policy = "Public")]
    public class ArcPublic;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Public")]
    public class AspNetPublic;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Default")]
    public class AspNetDefault;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Public", AuthenticationSchemes = "Missing")]
    public class AspNetScheme;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Authenticated")]
    public class AspNetAuthenticated;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Private")]
    public class AspNetPrivate;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Public")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Default")]
    public class Stacked;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "NativeOpted")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspNetDefault")]
    public class NativeOptedAspNetDefault;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Public")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "NativeDefault")]
    public class AspNetOptedNativeDefault;

    public class GuestNativePolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
