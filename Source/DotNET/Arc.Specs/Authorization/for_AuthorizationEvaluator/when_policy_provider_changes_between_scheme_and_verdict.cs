// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using AspNetAuthorizationResult = Microsoft.AspNetCore.Authorization.AuthorizationResult;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_policy_provider_changes_between_scheme_and_verdict : Specification
{
    int _lookups;
    bool _authorized;
    string? _selectedScheme;
    IEnumerable<IAuthorizationRequirement>? _evaluatedRequirements;
    AuthorizationPolicy _firstPolicy;

    async Task Because()
    {
        _firstPolicy = new AuthorizationPolicyBuilder("Special").RequireAuthenticatedUser().Build();
        var secondPolicy = new AuthorizationPolicyBuilder("Other").RequireAssertion(_ => false).Build();
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetPolicyAsync("Changing").Returns(_ =>
        {
            var resolved = Interlocked.Increment(ref _lookups) == 1 ? _firstPolicy : secondPolicy;
            return Task.FromResult<AuthorizationPolicy?>(resolved);
        });
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Special").Returns(Task.FromResult<AuthenticationScheme?>(new AuthenticationScheme("Special", "Special", typeof(IAuthenticationHandler))));
        var selected = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "selected")], "Special"));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Special")
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(selected, "Special"))));
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext.Returns(new DefaultHttpContext());
        var authorization = new RecordingAuthorizationService();
        await using var services = new ServiceCollection()
            .AddSingleton(provider)
            .AddSingleton(schemes)
            .AddSingleton(authentication)
            .AddSingleton(contextAccessor)
            .AddSingleton<IAuthorizationService>(authorization)
            .BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime([]));
        var resolution = await runtime.Resolve([AuthorizationRequirement.FromAttribute(null, "Changing", null)], services, CancellationToken.None);
        var selectedPrincipal = await resolution.SelectPrincipal(new ClaimsPrincipal(), services, CancellationToken.None);
        _selectedScheme = selectedPrincipal?.Identity?.AuthenticationType;
        _authorized = await resolution.IsAuthorized(new AuthorizationPolicyContext(selectedPrincipal!, typeof(object), new object()), services, CancellationToken.None);
        _evaluatedRequirements = authorization.LastRequirements;
    }

    [Fact] void should_authenticate_the_scheme_from_the_first_resolution() => _selectedScheme.ShouldEqual("Special");
    [Fact] void should_evaluate_that_same_policy_requirements() => ReferenceEquals(_evaluatedRequirements, _firstPolicy.Requirements).ShouldBeTrue();
    [Fact] void should_not_ask_the_provider_again_during_authorization() => _lookups.ShouldEqual(1);
    [Fact] void should_authorize_the_stable_snapshot() => _authorized.ShouldBeTrue();

    class RecordingAuthorizationService : IAuthorizationService
    {
        public IEnumerable<IAuthorizationRequirement>? LastRequirements { get; private set; }

        public Task<AspNetAuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            LastRequirements = requirements;
            return Task.FromResult(AspNetAuthorizationResult.Success());
        }

        public Task<AspNetAuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
            Task.FromResult(AspNetAuthorizationResult.Success());
    }
}
