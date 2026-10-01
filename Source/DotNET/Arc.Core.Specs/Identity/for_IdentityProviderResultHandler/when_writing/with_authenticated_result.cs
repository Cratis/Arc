// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_writing;

public class with_authenticated_result : given.an_authenticated_user
{
    IdentityProviderResult _identityProviderResult;

    void Establish()
    {
        _identityProviderResult = new IdentityProviderResult("user-123", "Test User", true, true, [], new { role = "Admin" });
        _httpRequestContext.IsHttps.Returns(true);
    }

    async Task Because() => await _handler.SetCookieForHttpResponse(_identityProviderResult);

    [Fact] void should_set_no_store_cache_control() => _httpRequestContext.Received(1).SetResponseHeader("Cache-Control", "no-store, private");
    [Fact] void should_vary_on_cookie() => _httpRequestContext.Received(1).SetResponseHeader("Vary", "Cookie");
    [Fact] void should_set_content_type() => _httpRequestContext.ContentType.ShouldEqual("application/json; charset=utf-8");
    [Fact] void should_not_expire_a_cookie_the_request_does_not_carry() => _httpRequestContext.DidNotReceive().RemoveCookie(Arg.Any<string>());
    [Fact] void should_not_write_an_identity_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Http.CookieOptions>());
    [Fact] void should_write_json_response() => _httpRequestContext.Received(1).Write(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
