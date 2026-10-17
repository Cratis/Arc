// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AspNetCoreEndpointFallbackAuthorization;

public class when_a_fallback_policy_selects_a_scheme : Specification
{
    readonly ClaimsPrincipal _principal = new(new ClaimsIdentity());
    IAuthenticationService _authenticationService;
    IAuthorizationService _authorizationService;
    HttpContext _evaluationContext;
    AuthorizationPolicy _policy;
    DefaultHttpContext _nativeContext;
    IHttpRequestContext _context;
    ServiceProvider _services;
    AuthenticateResult _authentication;
    bool _authorized;

    void Establish()
    {
        _policy = new AuthorizationPolicyBuilder("Session").RequireAuthenticatedUser().Build();
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetFallbackPolicyAsync().Returns(_policy);
        var accessor = new HttpContextAccessor();
        _authenticationService = Substitute.For<IAuthenticationService>();
        _authorizationService = Substitute.For<IAuthorizationService>();
        _authentication = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([], "Session")), "Session"));
        _authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), "Session").Returns(call =>
        {
            _evaluationContext = call.Arg<HttpContext>();
            return Task.FromResult(_authentication);
        });
        _authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(AuthorizationResult.Success());
        _services = new ServiceCollection()
            .AddSingleton(provider)
            .AddSingleton(_authenticationService)
            .AddSingleton<IPolicyEvaluator>(new PolicyEvaluator(_authorizationService))
            .AddSingleton<IHttpContextAccessor>(accessor)
            .BuildServiceProvider();
        _nativeContext = new DefaultHttpContext { RequestServices = _services, User = _principal };
        _nativeContext.Request.Path = "/.cratis/queries/ws";
        accessor.HttpContext = _nativeContext;
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestServices.Returns(_services);
    }

    async Task Because() => _authorized = await new AspNetCoreEndpointFallbackAuthorization().IsAuthorized(_context, _principal, CancellationToken.None);

    void Destroy() => _services.Dispose();

    [Fact] void should_admit_the_selected_scheme() => _authorized.ShouldBeTrue();
    [Fact] void should_authenticate_against_an_isolated_facade() => _evaluationContext.ShouldNotBeSame(_nativeContext);
    [Fact] void should_preserve_the_original_user() => _nativeContext.User.ShouldBeSame(_principal);
    [Fact] void should_preserve_the_original_services() => _nativeContext.RequestServices.ShouldBeSame(_services);
    [Fact] void should_use_the_original_services_on_the_facade() => _evaluationContext.RequestServices.ShouldBeSame(_services);
    [Fact] void should_keep_the_live_request_features() => _evaluationContext.Request.Path.ShouldEqual(_nativeContext.Request.Path);
    [Fact] void should_select_the_scheme_on_the_facade() => _evaluationContext.User.Identity!.AuthenticationType.ShouldEqual("Session");
    [Fact] void should_authorize_with_the_facade_as_resource() => _authorizationService.Received(1).AuthorizeAsync(_evaluationContext.User, _evaluationContext, _policy.Requirements);
}
