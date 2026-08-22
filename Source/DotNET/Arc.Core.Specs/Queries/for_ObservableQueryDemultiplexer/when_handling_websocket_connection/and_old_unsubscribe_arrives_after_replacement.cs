// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_old_unsubscribe_arrives_after_replacement : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _queryIds = [FirstQueryId, FirstQueryId];
        _subscriptionRevisions = [1, 2];
        _queryIdToUnsubscribe = FirstQueryId;
        _unsubscribeRevision = 1;
    }

    Task Because() => RunConnection(
        () => Task.CompletedTask,
        async () =>
        {
            _subject.OnNext(["current"]);
            await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
        });

    [Fact] void should_keep_the_replacement_streaming() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
}
