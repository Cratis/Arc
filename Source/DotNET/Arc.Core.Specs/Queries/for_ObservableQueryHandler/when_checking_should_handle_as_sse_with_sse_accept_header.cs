// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_checking_should_handle_as_sse_with_sse_accept_header : given.an_observable_query_handler
{
    IHttpRequestContext _context;
    bool _result;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.Headers.Returns(new Dictionary<string, string> { { "Accept", "text/event-stream" } });
    }

    void Because() => _result = _handler.ShouldHandleAsSSE(_context);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
