// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result;

/// <summary>
/// This is the path that was broken outright: the handler passed the query context to a
/// <see cref="ClientEnumerableObservable{T}"/> whose constructor did not take one, so every model-bound
/// <see cref="IAsyncEnumerable{T}"/> observable query over WebSocket threw on connect.
/// </summary>
public class and_an_async_enumerable_is_requested_over_websocket : given.a_handler_over_a_real_container
{
    readonly TaskCompletionSource _incomingCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Exception _error;

    void Establish()
    {
        ConnectOverWebSocket();

        // Stand in for a client that stays connected until the server ends the connection — the observable ends it
        // once the stream is exhausted, and returning immediately here would instead cancel the stream mid-flight.
        _webSocketConnectionHandler
            .HandleIncomingMessages(Arg.Any<IWebSocket>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().Register(() => _incomingCompleted.TrySetResult());
                return _incomingCompleted.Task;
            });
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.HandleStreamingResult(_context, StreamingQueryName, TwoItems()));

    [Fact] void should_construct_the_observable() => _error.ShouldBeNull();
    [Fact] void should_write_every_item_to_the_client() => _sent.Count.ShouldEqual(2);
}
