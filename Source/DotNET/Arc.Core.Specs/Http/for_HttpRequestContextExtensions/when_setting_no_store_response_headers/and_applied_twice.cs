// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers;

public class and_applied_twice : given.a_context_tracking_response_headers
{
    void Establish() => _responseHeaders["Vary"] = "Origin";

    void Because()
    {
        _context.SetNoStoreResponseHeaders();
        _context.SetNoStoreResponseHeaders();
    }

    [Fact] void should_add_cookie_only_once() => _responseHeaders["Vary"].ShouldEqual("Origin, Cookie");
}
