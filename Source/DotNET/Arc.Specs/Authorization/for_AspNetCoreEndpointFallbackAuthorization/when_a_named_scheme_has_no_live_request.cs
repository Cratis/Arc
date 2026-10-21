// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AspNetCoreEndpointFallbackAuthorization;

public class when_a_named_scheme_has_no_live_request : Specification
{
    ServiceProvider _services;
    IHttpRequestContext _context;
    bool _authorized;

    void Establish()
    {
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetFallbackPolicyAsync().Returns(new AuthorizationPolicyBuilder("Session").RequireAuthenticatedUser().Build());
        _services = new ServiceCollection().AddSingleton(provider).BuildServiceProvider();
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestServices.Returns(_services);
    }

    async Task Because() => _authorized = await new AspNetCoreEndpointFallbackAuthorization().IsAuthorized(
        _context,
        new ClaimsPrincipal(new ClaimsIdentity([], "other-scheme")),
        CancellationToken.None);

    void Destroy() => _services.Dispose();

    [Fact] void should_not_fall_back_to_the_captured_authenticated_principal() => _authorized.ShouldBeFalse();
}
