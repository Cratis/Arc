// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_the_same_id_across_legacy_streams_without_concurrency : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;
    readonly TestEvent _first = new("first");
    readonly TestEvent _second = new("second");

    void Establish() => _id = EventSourceId.New();

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, _first) { EventStreamType = new EventStreamType("Transactions") },
        new(_id, _second) { EventStreamType = new EventStreamType("Postings") }
    });

    [Fact] void should_append_each_event_separately() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(2);
    [Fact] void should_leave_each_route_guard_to_chronicle() => Received.InOrder(() =>
    {
        _eventLog.Append(_id, _first, new EventStreamType("Transactions"), concurrencyScope: null);
        _eventLog.Append(_id, _second, new EventStreamType("Postings"), concurrencyScope: null);
    });
    [Fact] void should_not_append_a_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(0);
}
