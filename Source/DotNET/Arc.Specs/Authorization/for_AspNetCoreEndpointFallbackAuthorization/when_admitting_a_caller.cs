// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AspNetCoreEndpointFallbackAuthorization;

public class when_admitting_a_caller
{
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, true)]
    public async Task should_apply_the_fallback_policy_with_or_without_a_live_request(bool hasFallback, bool authenticated, bool liveRequest, bool expected)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "test" : null));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddAuthorization(options =>
        {
            if (hasFallback)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
        });
        var accessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        await using var provider = services.BuildServiceProvider();
        accessor.HttpContext = liveRequest ? new DefaultHttpContext { RequestServices = provider, User = principal } : null;
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(provider);

        var authorized = await new AspNetCoreEndpointFallbackAuthorization().IsAuthorized(context, principal, CancellationToken.None);

        authorized.ShouldEqual(expected);
    }
}
