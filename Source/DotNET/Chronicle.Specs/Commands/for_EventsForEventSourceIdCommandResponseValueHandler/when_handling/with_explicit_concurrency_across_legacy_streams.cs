// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_explicit_concurrency_across_legacy_streams : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;
    ConcurrencyScope _scope;

    void Establish()
    {
        _id = EventSourceId.New();
        _commandContext = new(_correlationId, typeof(DeclaringCommand), new DeclaringCommand(), [], new CommandContextValues(), null);
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");
        _scope = new(EventSequenceNumber.First, _id, null, new EventStreamId("command-stream"), null, null);
        _concurrencyScopeStrategy.GetScope(_id, null, new EventStreamId("command-stream"), null, null).Returns(_scope);
    }

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, new TestEvent("first")) { EventStreamType = new EventStreamType("Transactions") },
        new(_id, new TestEvent("second")) { EventStreamType = new EventStreamType("Postings") }
    });

    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_keep_both_streams() => _appended.Select(_ => _.EventStreamType).ShouldEqual([new EventStreamType("Transactions"), new EventStreamType("Postings")]);
    [Fact] void should_keep_the_explicit_scope() => _scopes[_id].ShouldEqual(_scope);
    [Fact] void should_resolve_the_scope_once() => _concurrencyScopeStrategy.Received(1).GetScope(_id, null, new EventStreamId("command-stream"), null, null);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);

    [EventStreamId(concurrency: true)]
    public class DeclaringCommand;
}
