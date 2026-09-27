// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers;

public class with_existing_cache_control : given.a_context_tracking_response_headers
{
    void Establish() => _responseHeaders["Cache-Control"] = "public, max-age=60";

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_replace_cache_control_with_no_store() => _responseHeaders["Cache-Control"].ShouldEqual("no-store, private");
}
