// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions;

public class when_setting_sse_response_headers : Specification
{
    IHttpRequestContext _context;

    void Establish() => _context = Substitute.For<IHttpRequestContext>();

    void Because() => _context.SetSseResponseHeaders();

    [Fact] void should_set_content_type_to_sse() => _context.ContentType.ShouldEqual("text/event-stream; charset=utf-8");
    [Fact] void should_set_cache_control_header() => _context.Received().SetResponseHeader("Cache-Control", "no-cache");
    [Fact] void should_set_connection_header() => _context.Received().SetResponseHeader("Connection", "keep-alive");
    [Fact] void should_set_accel_buffering_header() => _context.Received().SetResponseHeader("X-Accel-Buffering", "no");
}
