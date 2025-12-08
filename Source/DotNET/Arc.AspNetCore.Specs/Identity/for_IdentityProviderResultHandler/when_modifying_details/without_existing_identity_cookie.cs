// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class without_existing_identity_cookie : given.an_identity_provider_result_handler
{
    string _responseContent;
    IList<Microsoft.Net.Http.Headers.SetCookieHeaderValue> _cookies;

    async Task Because()
    {
        await _handler.ModifyDetails<object>(details => new { Modified = true });

        _httpContext.Response.Body.Position = 0;
        var reader = new StreamReader(_httpContext.Response.Body);
        _responseContent = await reader.ReadToEndAsync();

        _cookies = _httpContext.Response.GetTypedHeaders().SetCookie;
    }

    [Fact] void should_not_write_anything_to_response() => _responseContent.ShouldBeEmpty();

    [Fact] void should_not_set_any_cookies() => _cookies.ShouldBeEmpty();
}