// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_setting_no_store_response_headers;

public class with_existing_vary_origin : given.a_default_http_context
{
    void Establish() => _httpContext.Response.Headers.Vary = "Origin";

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_keep_origin_and_add_cookie_on_the_response() => _httpContext.Response.Headers.Vary.ToString().ShouldEqual("Origin, Cookie");
    [Fact] void should_set_no_store_cache_control_on_the_response() => _httpContext.Response.Headers.CacheControl.ToString().ShouldEqual("no-store, private");
}
