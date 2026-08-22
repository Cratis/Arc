// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

/// <summary>
/// A subject emission can already be awaiting transport output when the client unsubscribes while keeping the socket
/// open. The operation must use the subject subscription token, so unsubscription cancels the pending send before
/// connection-wide teardown and the unsent result is not tracked as served.
/// </summary>
public class and_subject_send_is_cancelled_when_unsubscribed : given.a_guarded_websocket_connection
{
    readonly TaskCompletionSource _sendCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _sendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    bool _sendStoppedBeforeConnectionClosed;

    void Establish()
    {
        _queryIdToUnsubscribe = FirstQueryId;
        _webSocket.Send(
                Arg.Any<ArraySegment<byte>>(),
                Arg.Any<System.Net.WebSockets.WebSocketMessageType>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => BlockSend(callInfo.Arg<CancellationToken>()));
    }

    async Task Because() => await RunConnection(
        async () =>
        {
            _subject.OnNext(["item-a"]);
            await _sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        },
        async () =>
        {
            await _sendCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            _sendStoppedBeforeConnectionClosed = true;
        });

    [Fact] void should_cancel_the_send_before_connection_teardown() => _sendStoppedBeforeConnectionClosed.ShouldBeTrue();
    [Fact] void should_not_send_the_late_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_track_data_as_served() => _healthTracker.DidNotReceive().RecordDataServed(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_only_unregister_the_explicitly_unsubscribed_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

    async Task BlockSend(CancellationToken token)
    {
        _sendStarted.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, token);
        }
        finally
        {
            _sendCompleted.TrySetResult();
        }
    }
}
