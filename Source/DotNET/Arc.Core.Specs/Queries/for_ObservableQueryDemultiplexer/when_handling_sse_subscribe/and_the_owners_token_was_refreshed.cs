// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// A connection is held open for as long as the client stays on the page, which is long enough for the caller's
/// token to be renewed underneath it. Ownership is bound to the identity, not to the principal instance or the
/// display name it happened to carry when the stream was opened.
/// </summary>
public class and_the_owners_token_was_refreshed : given.an_sse_connection_owned_by_a_caller
{
    async Task Because() => await RunConnection(async () =>
    {
        await _hub.HandleSSESubscribe(SubscribeContextFor(_ownerAfterATokenRefresh));
        _subject.OnNext(["an-item"]);
        await WaitFor(() => CountQueryResults() > 0);
    });

    [Fact] void should_accept_the_subscription() => _statusCode.ShouldEqual(200);
    [Fact] void should_stream_results_to_the_connection() => CountQueryResults().ShouldBeGreaterThan(0);
}
