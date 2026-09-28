// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AspNetAuthorizationPolicyRuntime;

public class when_authorizing_with_an_already_cancelled_token : Specification
{
    Exception _error;
    int _accessorResolutions;
    IAuthorizationService _authorization;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var policy = new AuthorizationPolicyBuilder().RequireClaim("policy", "first").Build();
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetPolicyAsync("First").Returns(Task.FromResult<AuthorizationPolicy?>(policy));
        _authorization = Substitute.For<IAuthorizationService>();
        await using var services = new ServiceCollection()
            .AddSingleton(provider)
            .AddSingleton(_authorization)
            .AddTransient(_ =>
            {
                _accessorResolutions++;
                return Substitute.For<ICurrentPrincipalAccessor>();
            })
            .BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime([]));
        var resolution = await runtime.Resolve([AuthorizationRequirement.FromAttribute(null, "First", null)], services, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _error = await Catch.Exception(() => resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, cancellation.Token));
    }

    [Fact] void should_throw_for_the_cancellation() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_resolve_the_principal_accessor() => _accessorResolutions.ShouldEqual(0);
    [Fact] void should_not_evaluate_any_policy() => _authorization.ReceivedCalls().ShouldBeEmpty();
}
