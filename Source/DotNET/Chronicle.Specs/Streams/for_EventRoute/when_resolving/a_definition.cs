// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Streams.for_EventRoute.when_resolving;

public class a_definition : Specification
{
    EventRoute _route;
    EventForEventSourceId _routed;
    void Because()
    {
        var definition = new EventSourceDefinition(typeof(Report), "reports", "", ConcurrencyDimensions.EventSourceId, [new("approval", "", ConcurrencyDimensions.EventStreamId)]);
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(Report)).Returns(definition);
        _route = new EventRoutes(sources).For<Report>("approval").WithStreamId("owner:month");
        _routed = _route.Route(EventSourceId.New(), new object());
    }
    [Fact] void should_resolve_the_source_type() => _route.EventSourceType.Value.ShouldEqual("reports");
    [Fact] void should_resolve_the_stream_policy() => _route.Concurrency.ShouldEqual(ConcurrencyDimensions.EventStreamId);
    [Fact] void should_stamp_the_definition() => _routed.EventSource.ShouldEqual(typeof(Report));
    [Fact] void should_stamp_the_stream_name() => _routed.EventStream.ShouldEqual("approval");
    [Fact] void should_stamp_the_source_type() => _routed.EventSourceType.ShouldEqual(_route.EventSourceType);
    [Fact] void should_stamp_the_stream_type() => _routed.EventStreamType.ShouldEqual(_route.EventStreamType);
    [Fact] void should_stamp_the_stream_id() => _routed.EventStreamId.ShouldEqual(_route.EventStreamId);
    class Report : IEventSource;
}
