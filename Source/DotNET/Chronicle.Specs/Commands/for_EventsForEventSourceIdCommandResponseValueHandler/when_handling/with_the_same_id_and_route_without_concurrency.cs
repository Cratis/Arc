// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_the_same_id_and_route_without_concurrency : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;

    void Establish() => _id = EventSourceId.New();

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, new TestEvent("first")) { EventStreamType = new EventStreamType("Transactions") },
        new(_id, new TestEvent("second")) { EventStreamType = new EventStreamType("Transactions") }
    });

    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_keep_both_events() => _appended.Select(_ => _.Event).ShouldEqual([new TestEvent("first"), new TestEvent("second")]);
    [Fact] void should_leave_the_guard_to_chronicle() => _scopes[_id].ShouldEqual(ConcurrencyScope.NotSet);
    [Fact] void should_carry_one_scope() => _scopes.Count.ShouldEqual(1);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
