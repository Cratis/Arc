// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_writing;

/// <summary>
/// Earlier versions wrote a readable identity cookie. A frontend from those versions reads it before asking the server,
/// so it is expired the next time the identity is written - otherwise it would keep showing whatever it last held.
/// </summary>
public class with_a_legacy_identity_cookie_on_the_request : given.an_authenticated_user
{
    void Establish() => _httpRequestContext.Cookies.Returns(given.forged_identity_cookie.AsCookies(_options));

    async Task Because() => await _handler.SetCookieForHttpResponse(new IdentityProviderResult("user-123", "Test User", true, true, [], new { role = "Admin" }));

    [Fact] void should_expire_the_legacy_cookie() => _httpRequestContext.Received(1).RemoveCookie(given.forged_identity_cookie.Name);
    [Fact] void should_not_write_an_identity_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Http.CookieOptions>());
}
