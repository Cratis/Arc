// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_query_is_in_the_endpoint_group_the_port_serves : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _performer.Type.Returns(typeof(InternalReadModel));
        _performer.Name.Returns(new QueryName("AllItems"));
        _context.Items.Returns(new Dictionary<object, object?>());
        _context.SetEndpointGroupScope(new EndpointGroupScope(false, ["internal"]));
        _streamingData = new[] { "snapshot" };
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_not_answer_with_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_send_the_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);

    [EndpointGroup("internal")]
    public record InternalReadModel;
}
