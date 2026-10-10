// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_a_stream_type_read : given.a_stream_decision
{
    void Establish()
    {
        _scenario.Given.ForEventSource(_id).OnRoute(_route.WithStreamId("sibling")).Events(new PriorFact("sibling"));
        _scenario.Given.ForEventSource(_id).OnRoute(_route with { EventStreamType = "other" }).Events(new PriorFact("other-type"));
    }
    async Task Because() => _result = await _scenario.Execute(new(_id, WholeType: true));
    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_read_sibling_stream_ids_of_the_type() => ((Approved)_scenario.AppendedEvents.Single().Event.Content).PriorCount.ShouldEqual(1);
    [Fact] void should_append_to_the_specific_target_id() => _scenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(_route.EventStreamId);
}
