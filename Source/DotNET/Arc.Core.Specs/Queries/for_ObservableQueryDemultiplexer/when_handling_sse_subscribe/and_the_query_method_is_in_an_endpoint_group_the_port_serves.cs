// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_query_method_is_in_an_endpoint_group_the_port_serves : given.a_guarded_sse_connection
{
    void Establish()
    {
        _performer.Type.Returns(typeof(Model));
        _performer.Name.Returns(new QueryName(nameof(Model.Query)));
        _queryIds = [];
        _streamingData = new[] { "snapshot" };
        _connectionContext.Items.Returns(new Dictionary<object, object?>());
        _connectionContext.SetEndpointGroupScope(new EndpointGroupScope(false, ["internal"]));
    }

    async Task Because() => await RunConnection(async () =>
    {
        var context = CreateSubscribeContext(FirstQueryId);
        context.Items.Returns(new Dictionary<object, object?>());
        context.SetEndpointGroupScope(new EndpointGroupScope(false, ["internal"]));
        await _hub.HandleSSESubscribe(context);
    });

    [Fact] void should_accept_the_subscription() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(200);
    [Fact] void should_not_answer_with_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_send_the_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }
}
