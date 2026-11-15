// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_a_subscriber_is_slower_than_the_producer;

public class and_the_sender_is_stalled : given.a_guarded_websocket_connection
{
    const int Capacity = 3;

    TaskCompletionSource _release;
    bool _stillObservedAfterOverflow;

    void Establish()
    {
        _arcOptions.Value.Query.MaxQueuedEmissionsPerSubscription = Capacity;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _beforeSendCompletes = message => message.Type == ObservableQueryHubMessageType.QueryResult ? _release.Task : Task.CompletedTask;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["first"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);

        for (var index = 0; index < 10; index++)
        {
            _subject.OnNext([$"queued-{index}"]);
        }

        await WaitFor(() => _unregisteredCount == 1);
        _stillObservedAfterOverflow = _subject.HasObservers;
        _release.SetResult();
    });

    [Fact] void should_start_at_most_one_send() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_detach_from_the_producer() => _stillObservedAfterOverflow.ShouldBeFalse();
    [Fact] void should_complete_the_subscription() => _unregisteredCount.ShouldEqual(1);
}
