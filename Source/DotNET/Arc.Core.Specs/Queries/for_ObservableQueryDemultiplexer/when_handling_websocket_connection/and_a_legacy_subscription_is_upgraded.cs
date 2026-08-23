// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_a_legacy_subscription_is_upgraded : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _queryIds = [FirstQueryId, FirstQueryId];
        _subscriptionRevisions = [null, 1];
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
    });

    [Fact] void should_cancel_the_temporary_legacy_subscription() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_send_only_the_revision_aware_result() =>
        ((long?)typeof(ObservableQueryHubMessage).GetProperty("Revision")!.GetValue(_sentMessages.Single(_ => _.Type == ObservableQueryHubMessageType.QueryResult))).ShouldEqual(1L);
}
