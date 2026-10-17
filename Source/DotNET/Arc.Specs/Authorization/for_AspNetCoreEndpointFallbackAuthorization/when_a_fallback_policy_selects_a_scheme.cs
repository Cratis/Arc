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
    IPolicyEvaluator _evaluator;
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
        _evaluator = Substitute.For<IPolicyEvaluator>();
        _authentication = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([], "Session")), "Session"));
        _evaluator.AuthenticateAsync(_policy, Arg.Any<HttpContext>()).Returns(_authentication);
        _evaluator.AuthorizeAsync(_policy, _authentication, Arg.Any<HttpContext>(), Arg.Any<object>()).Returns(PolicyAuthorizationResult.Success());
        _services = new ServiceCollection().AddSingleton(provider).AddSingleton(_evaluator).AddSingleton<IHttpContextAccessor>(accessor).BuildServiceProvider();
        _nativeContext = new DefaultHttpContext { RequestServices = _services, User = _principal };
        accessor.HttpContext = _nativeContext;
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestServices.Returns(_services);
    }

    async Task Because() => _authorized = await new AspNetCoreEndpointFallbackAuthorization().IsAuthorized(_context, _principal, CancellationToken.None);

    void Destroy() => _services.Dispose();

    [Fact] void should_admit_the_selected_scheme() => _authorized.ShouldBeTrue();
    [Fact] void should_authenticate_against_the_live_request() => _evaluator.Received(1).AuthenticateAsync(_policy, _nativeContext);
    [Fact] void should_authorize_with_the_live_request_as_resource() => _evaluator.Received(1).AuthorizeAsync(_policy, _authentication, _nativeContext, _nativeContext);
}
