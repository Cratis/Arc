// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_a_subscriber_is_slower_than_the_producer;

public class and_emissions_stay_within_capacity : given.a_guarded_websocket_connection
{
    const int Capacity = 3;

    TaskCompletionSource _release;

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
        _subject.OnNext(["second"]);
        _subject.OnNext(["third"]);
        _release.SetResult();
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 3);
    });

    [Fact] void should_deliver_every_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(3);
    [Fact] void should_not_report_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_terminate_the_subscription_early() => _unregisteredCount.ShouldEqual(1);
}
