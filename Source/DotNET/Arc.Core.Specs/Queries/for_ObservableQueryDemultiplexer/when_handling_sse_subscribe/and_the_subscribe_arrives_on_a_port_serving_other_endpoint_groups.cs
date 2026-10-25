// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_subscribe_arrives_on_a_port_serving_other_endpoint_groups : given.an_sse_connection_owned_by_a_caller
{
    void Establish()
    {
        _connectionContext.Items.Returns(new Dictionary<object, object?>());
        _connectionContext.SetEndpointGroupScope(EndpointGroupScope.Default);
    }

    async Task Because() => await RunConnection(async () =>
    {
        var subscribe = SubscribeContextFor(_owner);
        subscribe.Items.Returns(new Dictionary<object, object?>());
        subscribe.SetEndpointGroupScope(new EndpointGroupScope(false, ["internal"]));
        await _hub.HandleSSESubscribe(subscribe);
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
}
