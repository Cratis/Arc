// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_caller_does_not_own_the_connection : given.an_sse_connection_owned_by_a_caller
{
    async Task Because() => await RunConnection(async () =>
    {
        await _hub.HandleSSESubscribe(SubscribeContextFor(_anotherCaller));
        _subject.OnNext(["an-item"]);
    });

    [Fact] void should_answer_as_though_the_connection_does_not_exist() => _statusCode.ShouldEqual(404);

    [Fact]
    void should_not_perform_the_query() =>
        _queryPipeline.DidNotReceive().Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());

    [Fact] void should_not_write_anything_to_the_owners_stream() => CountQueryResults().ShouldEqual(0);
}
