// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_setting_no_store_response_headers;

public class with_multiple_existing_vary_values : given.a_default_http_context
{
    void Establish() => _httpContext.Response.Headers.Vary = new StringValues(["Origin", "Accept-Encoding"]);

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_keep_all_values_and_add_cookie_on_the_response() => _httpContext.Response.Headers.Vary.ToString().ShouldEqual("Origin, Accept-Encoding, Cookie");
}
