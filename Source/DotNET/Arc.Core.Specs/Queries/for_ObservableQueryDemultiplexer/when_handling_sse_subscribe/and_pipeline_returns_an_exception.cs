// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_pipeline_returns_an_exception : given.an_sse_connection_owned_by_a_caller
{
    ObservableQueryHubMessage _received;

    void Establish() => _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(QueryResult.Error(CorrelationId.New(), "Query failed.")));

    async Task Because()
    {
        await RunConnection(async () => await _hub.HandleSSESubscribe(SubscribeContextFor(_owner)));
        _received = _messages.Select(text => JsonSerializer.Deserialize<ObservableQueryHubMessage>(text["data: ".Length..].Trim(), _arcOptions.Value.JsonSerializerOptions))
            .Single(message => message!.Type == ObservableQueryHubMessageType.Error)!;
    }

    [Fact] void should_send_an_error_frame() => _received.Type.ShouldEqual(ObservableQueryHubMessageType.Error);
    [Fact] void should_preserve_the_exception_message() => ((JsonElement)_received.Payload!).GetString().ShouldEqual("Query failed.");
    [Fact] void should_not_send_a_query_result() => CountQueryResults().ShouldEqual(0);
}
