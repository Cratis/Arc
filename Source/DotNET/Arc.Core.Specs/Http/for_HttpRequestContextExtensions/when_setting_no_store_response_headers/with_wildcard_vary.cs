// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers;

public class with_wildcard_vary : given.a_context_tracking_response_headers
{
    void Establish() => _responseHeaders["Vary"] = "*";

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_keep_the_wildcard() => _responseHeaders["Vary"].ShouldEqual("*");
    [Fact] void should_set_no_store_cache_control() => _responseHeaders["Cache-Control"].ShouldEqual("no-store, private");
}
