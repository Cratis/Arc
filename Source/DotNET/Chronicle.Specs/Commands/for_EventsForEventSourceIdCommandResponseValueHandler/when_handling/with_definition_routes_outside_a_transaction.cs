// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_definition_routes_outside_a_transaction : given.an_events_for_event_source_id_command_response_value_handler
{
    void Establish()
    {
        _commandContext.Values[WellKnownCommandContextKeys.EventSource] = typeof(MultiSourceBatchAccount);
        _commandContext.Values[WellKnownCommandContextKeys.EventStream] = "Transactions";
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = new EventSourceType("MultiSourceBatchAccount");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = new EventStreamType("Transactions");
    }

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(EventSourceId.New(), new TestEvent("posting")) { EventStream = "Postings", EventStreamId = new EventStreamId("period") },
        new(EventSourceId.New(), new TestEvent("ledger")) { EventSource = typeof(MultiSourceBatchLedger), EventStream = "Entries" }
    });

    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_keep_the_command_definition_with_the_overridden_stream() => (_appended[0].EventSource, _appended[0].EventStream).ShouldEqual((typeof(MultiSourceBatchAccount), "Postings"));
    [Fact] void should_keep_the_overridden_definition_and_stream() => (_appended[1].EventSource, _appended[1].EventStream).ShouldEqual((typeof(MultiSourceBatchLedger), "Entries"));
    [Fact] void should_keep_the_stream_id() => _appended[0].EventStreamId.ShouldEqual(new EventStreamId("period"));
    [Fact] void should_leave_source_types_to_the_definitions() => _appended.ShouldEachConformTo(_ => _.EventSourceType == EventSourceType.Default);
    [Fact] void should_leave_stream_types_to_the_definitions() => _appended.ShouldEachConformTo(_ => _.EventStreamType == EventStreamType.All);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append) || _.GetMethodInfo().Name == nameof(_eventLog.AppendThroughEventSource)).ShouldEqual(0);

    [EventSource]
    [EventStream("Transactions")]
    [EventStream("Postings")]
    public class MultiSourceBatchAccount : IEventSource;

    [EventSource]
    [EventStream("Entries")]
    public class MultiSourceBatchLedger : IEventSource;
}
