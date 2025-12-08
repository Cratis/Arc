// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_writing;

public class with_http_request : given.an_identity_provider_result_handler
{
    IdentityProviderResult _identityResult;
    Microsoft.Net.Http.Headers.SetCookieHeaderValue _identityCookie;

    void Establish()
    {
        _identityResult = new IdentityProviderResult(
            new IdentityId("user123"),
            new IdentityName("Test User"),
            true,
            true,
            new { Department = "Engineering" });

        _httpContext.Request.Scheme = "http";
    }

    async Task Because()
    {
        await _handler.Write(_identityResult);

        var cookies = _httpContext.Response.GetTypedHeaders().SetCookie;
        _identityCookie = cookies.FirstOrDefault(c => c.Name == IdentityProviderResultHandler.IdentityCookieName)!;
    }

    [Fact] void should_set_cookie_as_not_secure_when_request_is_http() => _identityCookie.Secure.ShouldBeFalse();
}