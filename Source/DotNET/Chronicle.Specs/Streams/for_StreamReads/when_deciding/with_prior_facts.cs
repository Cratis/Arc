// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_prior_facts : given.a_stream_decision
{
    void Establish()
    {
        _scenario.Given.ForEventSource(_id).OnRoute(_route).Events(new PriorFact("first"), new Approved(0, true));
        _scenario.Given.ForEventSource(_id).OnRoute(_route.WithStreamId("sibling")).Events(new PriorFact("sibling"));
        _scenario.Given.ForEventSource(_id).OnRoute(_route with { EventStreamType = "other" }).Events(new PriorFact("other"));
        _scenario.Given.ForEventSource(_id).OnRoute(_route with { EventSourceType = "other-source" }).Events(new PriorFact("other-source"));
    }

    async Task Because() => _result = await _scenario.Execute(new(_id));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_read_all_event_types_but_only_the_selected_route() => ((Approved)_scenario.AppendedEvents.Single().Event.Content).PriorCount.ShouldEqual(2);
    [Fact] void should_append_on_the_read_stream() => _scenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(_route.EventStreamId);
    [Fact] void should_append_on_the_read_stream_type() => _scenario.AppendedEvents.Single().Event.Context.EventStreamType.ShouldEqual(_route.EventStreamType);
    [Fact] void should_append_on_the_read_source_type() => _scenario.AppendedEvents.Single().Event.Context.EventSourceType.ShouldEqual(_route.EventSourceType);
}
