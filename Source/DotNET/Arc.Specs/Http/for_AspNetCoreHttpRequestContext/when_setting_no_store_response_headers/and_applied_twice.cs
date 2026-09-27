// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_setting_no_store_response_headers;

public class and_applied_twice : given.a_default_http_context
{
    void Establish() => _httpContext.Response.Headers.Vary = "Origin";

    void Because()
    {
        _context.SetNoStoreResponseHeaders();
        _context.SetNoStoreResponseHeaders();
    }

    [Fact] void should_add_cookie_to_the_response_only_once() => _httpContext.Response.Headers.Vary.ToString().ShouldEqual("Origin, Cookie");
}
