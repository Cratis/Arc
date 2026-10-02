// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_per_event_routing_overrides : given.a_single_event_for_event_source_id_command_response_value_handler
{
    EventSourceId _eventSourceId;
    TestEvent _testEvent;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _testEvent = new TestEvent("Test Event");
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = new EventSourceType("Command");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = new EventStreamType("CommandStream");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream-id");
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = new Subject("command-subject");
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>()).Returns(AppendResult.Success(_correlationId, EventSequenceNumber.First));
    }

    Task Because() => _handler.Handle(
        _commandContext,
        new EventForEventSourceId(_eventSourceId, _testEvent)
        {
            EventSourceType = new EventSourceType("Ledger"),
            EventStreamType = new EventStreamType("Postings"),
            Subject = new Subject("event-subject")
        });

    [Fact] void should_use_the_routing_set_on_the_event() => _eventLog.Received(1).Append(
        _eventSourceId,
        _testEvent,
        new EventStreamType("Postings"),
        Arg.Any<EventStreamId?>(),
        new EventSourceType("Ledger"),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        new Subject("event-subject"));
    [Fact] void should_fall_back_to_the_command_stream_id_for_the_unset_value() => _eventLog.Received(1).Append(
        _eventSourceId,
        _testEvent,
        Arg.Any<EventStreamType?>(),
        new EventStreamId("command-stream-id"),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Subject?>());
}
