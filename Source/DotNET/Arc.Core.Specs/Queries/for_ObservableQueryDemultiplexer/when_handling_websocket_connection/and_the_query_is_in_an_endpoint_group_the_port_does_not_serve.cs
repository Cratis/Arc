// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_query_is_in_an_endpoint_group_the_port_does_not_serve : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _performer.Type.Returns(typeof(InternalReadModel));
        _performer.Name.Returns(new QueryName("AllItems"));
        _context.Items.Returns(new Dictionary<object, object?>());
        _context.SetEndpointGroupScope(EndpointGroupScope.Default);
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_answer_with_an_error() => HasErrorFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_run_the_query() => _correlationIds.ShouldBeEmpty();
    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);

    [EndpointGroup("internal")]
    public record InternalReadModel;
}
