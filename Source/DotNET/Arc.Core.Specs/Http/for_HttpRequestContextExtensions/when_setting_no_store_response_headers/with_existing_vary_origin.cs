// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers;

public class with_existing_vary_origin : given.a_context_tracking_response_headers
{
    void Establish() => _responseHeaders["Vary"] = "Origin";

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_keep_origin_and_add_cookie() => _responseHeaders["Vary"].ShouldEqual("Origin, Cookie");
}
