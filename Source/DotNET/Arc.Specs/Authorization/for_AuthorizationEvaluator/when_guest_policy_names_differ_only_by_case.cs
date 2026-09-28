// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_guest_policy_names_differ_only_by_case : given.both_attribute_families
{
    bool _optedIn;
    bool _notOptedIn;

    async Task Because()
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddAuthorization();
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        var optedInPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var notOptedInPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        provider.GetPolicyAsync("Guest").Returns(Task.FromResult<AuthorizationPolicy?>(optedInPolicy));
        provider.GetPolicyAsync("guest").Returns(Task.FromResult<AuthorizationPolicy?>(notOptedInPolicy));
        registrations.AddSingleton(provider);
        registrations.AddArcAnonymousAspNetAuthorizationPolicy("Guest");
        await using var services = registrations.BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(
            new ArcAuthorizationPolicyRuntime(services.GetServices<AuthorizationPolicyRegistration>()),
            services.GetServices<AnonymousAspNetAuthorizationPolicyRegistration>());
        await runtime.Validate([], services, CancellationToken.None);
        var evaluation = new AuthorizationEvaluation(ArcDeclarationsFirst(), ArcFirst(), _currentPrincipalAccessor, runtime);
        _optedIn = await evaluation.IsAuthorized(typeof(OptedIn), new object(), services, CancellationToken.None);
        _notOptedIn = await evaluation.IsAuthorized(typeof(NotOptedIn), new object(), services, CancellationToken.None);
    }

    [Fact] void should_allow_a_guest_through_the_exactly_opted_in_policy() => _optedIn.ShouldBeTrue();
    [Fact] void should_not_allow_a_guest_through_a_differently_cased_policy() => _notOptedIn.ShouldBeFalse();

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Guest")]
    public class OptedIn;

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "guest")]
    public class NotOptedIn;
}
