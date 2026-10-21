// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_EndpointFallbackAuthorization;

public class when_admitting_a_caller
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task should_require_authentication_only_with_registered_handlers(bool hasHandlers, bool authenticated, bool expected)
    {
        var authentication = Substitute.For<IAuthentication>();
        authentication.HasHandlers.Returns(hasHandlers);
        var fallback = new EndpointFallbackAuthorization(authentication);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "test" : null));

        var authorized = await fallback.IsAuthorized(Substitute.For<IHttpRequestContext>(), principal, CancellationToken.None);

        authorized.ShouldEqual(expected);
    }
}
