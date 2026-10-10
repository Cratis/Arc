// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_plain_and_routed_events : given.a_stream_decision
{
    CommandScenario<PlainAndRouted> _other;
    void Establish() => _other = new CommandScenario<PlainAndRouted>().UseDecisionReads();
    async Task Because() => _result = await _other.Execute(new(_id, new("owner"), "month"));
    async Task Destroy() => await _other.DisposeAsync();
    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_append_both_events_on_the_same_stream() => _other.AppendedEvents.Select(@event => @event.Event.Context.EventStreamId.Value).ShouldEqual("owner:month", "owner:month");
    [Fact] void should_append_both_events_through_the_definition() => _other.AppendedEvents.Select(@event => @event.Event.Context.EventSource.Value).ShouldEqual("reports", "reports");
}
