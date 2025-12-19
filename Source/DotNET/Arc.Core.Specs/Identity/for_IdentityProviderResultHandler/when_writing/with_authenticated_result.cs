// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_writing;

public class with_authenticated_result : given.an_authenticated_user
{
    IdentityProviderResult _identityProviderResult;

    void Establish()
    {
        _identityProviderResult = new IdentityProviderResult("user-123", "Test User", true, true, new { role = "Admin" });
        _httpRequestContext.IsHttps.Returns(true);
    }

    async Task Because() => await _handler.Write(_identityProviderResult);

    [Fact] void should_set_content_type() => _httpRequestContext.ContentType.ShouldEqual("application/json; charset=utf-8");
    [Fact] void should_append_cookie() => _httpRequestContext.Received(1).AppendCookie(
        IdentityProviderResultHandler.IdentityCookieName,
        Arg.Any<string>(),
        Arg.Is<Http.CookieOptions>(o =>
            !o.HttpOnly &&
            o.Secure &&
            o.SameSite == Http.SameSiteMode.Lax &&
            o.Path == "/"));
    [Fact] void should_write_json_response() => _httpRequestContext.Received(1).WriteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
