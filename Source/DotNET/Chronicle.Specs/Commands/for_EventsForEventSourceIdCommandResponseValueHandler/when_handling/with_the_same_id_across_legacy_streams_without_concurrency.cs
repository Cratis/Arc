// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

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

    [Fact] void should_append_all_events_in_one_batch() => _appended.Select(_ => _.Event).ShouldEqual([_first, _second]);
    [Fact] void should_append_exactly_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_preserve_each_route() => _appended.Select(_ => _.EventStreamType).ShouldEqual([new EventStreamType("Transactions"), new EventStreamType("Postings")]);
    [Fact] void should_leave_the_route_guards_to_chronicle() => _scopes[_id].ShouldEqual(ConcurrencyScope.NotSet);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
