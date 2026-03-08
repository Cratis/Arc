// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_handling_async_enumerable_via_http : given.an_observable_query_handler
{
    IHttpRequestContext _requestContext;
    int _statusCodeSet;
    bool _responseSentBeforeStatusCode;
    bool _responseWritten;

    void Establish()
    {
        _requestContext = Substitute.For<IHttpRequestContext>();
        _requestContext.RequestAborted.Returns(CancellationToken.None);
        _requestContext.WebSockets.IsWebSocketRequest.Returns(false);

        _requestContext.When(x => x.WriteResponseAsJson(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                _responseWritten = true;
                _responseSentBeforeStatusCode = _statusCodeSet == 0;
            });

        _requestContext.When(x => x.SetStatusCode(Arg.Any<int>()))
            .Do(ci => _statusCodeSet = ci.ArgAt<int>(0));
    }

    async Task Because() => await _handler.HandleStreamingResult(_requestContext, "TestQuery", new TestAsyncEnumerable());

    [Fact] void should_set_status_code_to_400() => _statusCodeSet.ShouldEqual(400);
    [Fact] void should_write_response() => _responseWritten.ShouldBeTrue();
    [Fact] void should_set_status_code_before_writing_response() => _responseSentBeforeStatusCode.ShouldBeFalse();

    class TestAsyncEnumerable : IAsyncEnumerable<string>
    {
        public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return "test";
        }
    }
}
