// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class with_invalid_base64_cookie : given.an_identity_provider_result_handler
{
    Exception _exception;

    void Establish()
    {
        _httpContext.Request.Cookies = new TestRequestCookieCollection();
        ((TestRequestCookieCollection)_httpContext.Request.Cookies).Add(IdentityProviderResultHandler.IdentityCookieName, "invalid-base64-content");
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.ModifyDetails<object>(details => new { Modified = true }));

    [Fact] void should_throw_format_exception() => _exception.ShouldBeOfExactType<FormatException>();
}