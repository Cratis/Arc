// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class with_invalid_json_in_cookie : given.an_identity_provider_result_handler
{
    Exception _exception;

    void Establish()
    {
        const string invalidJson = "{ invalid json content }";
        var base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidJson));

        _httpContext.Request.Cookies = new TestRequestCookieCollection();
        ((TestRequestCookieCollection)_httpContext.Request.Cookies).Add(IdentityProviderResultHandler.IdentityCookieName, base64Json);
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.ModifyDetails<object>(details => new { Modified = true }));

    [Fact] void should_throw_json_exception() => _exception.ShouldBeOfExactType<JsonException>();
}