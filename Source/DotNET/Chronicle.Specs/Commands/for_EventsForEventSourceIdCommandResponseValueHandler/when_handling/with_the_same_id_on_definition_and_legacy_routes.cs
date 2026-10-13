// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_the_same_id_on_definition_and_legacy_routes : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;
    readonly TestEvent _first = new("first");
    readonly TestEvent _second = new("second");

    void Establish()
    {
        _id = EventSourceId.New();
        _eventLog.Append(typeof(Account), _id, _first, "Transactions").Returns(AppendResult.Success(_correlationId, EventSequenceNumber.First));
    }

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, _first) { EventSource = typeof(Account), EventStream = "Transactions" },
        new(_id, _second) { EventStreamType = new EventStreamType("Transactions") }
    });

    [Fact] void should_append_each_event_separately() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append) || _.GetMethodInfo().Name == nameof(_eventLog.AppendThroughEventSource)).ShouldEqual(2);
    [Fact] void should_preserve_both_routes_without_attaching_scopes() => Received.InOrder(() =>
    {
        _eventLog.Append(typeof(Account), _id, _first, "Transactions", concurrencyScope: null);
        _eventLog.Append(_id, _second, new EventStreamType("Transactions"), concurrencyScope: null);
    });
    [Fact] void should_not_append_a_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(0);

    class Account;
}
