// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

/// <summary>
/// A subject emission can already be awaiting SSE transport output when the client unsubscribes while keeping the
/// connection open. The write must use the subject subscription token, and an unsent result must not be tracked.
/// </summary>
public class and_subject_write_is_cancelled_when_unsubscribed : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _writeCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _writeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    int _writeCount;

    void Establish()
    {
        _writeCount = 0;
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Write(callInfo.Arg<string>(), callInfo.Arg<CancellationToken>()));
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await _writeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Unsubscribe(FirstQueryId);
        await _writeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    });

    [Fact] void should_cancel_the_pending_write() => _writeCompleted.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_send_the_late_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_track_data_as_served() => _healthTracker.DidNotReceive().RecordDataServed(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_only_unregister_the_explicitly_unsubscribed_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

    Task Write(string message, CancellationToken token)
    {
        if (Interlocked.Increment(ref _writeCount) == 1)
        {
            _messages.Enqueue(message);
            return Task.CompletedTask;
        }

        return BlockWrite(token);
    }

    async Task BlockWrite(CancellationToken token)
    {
        _writeStarted.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, token);
        }
        finally
        {
            _writeCompleted.TrySetResult();
        }
    }
}
