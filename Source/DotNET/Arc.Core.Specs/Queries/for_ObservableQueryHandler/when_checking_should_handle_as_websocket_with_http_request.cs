// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_checking_should_handle_as_websocket_with_http_request : given.an_observable_query_handler
{
    IHttpRequestContext _context;
    bool _result;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        var webSocketContext = Substitute.For<IWebSocketContext>();
        webSocketContext.IsWebSocketRequest.Returns(false);
        _context.WebSockets.Returns(webSocketContext);
    }

    void Because() => _result = _handler.ShouldHandleAsWebSocket(_context);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
