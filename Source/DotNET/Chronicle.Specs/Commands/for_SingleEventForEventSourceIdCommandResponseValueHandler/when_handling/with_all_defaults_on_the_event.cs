// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_all_defaults_on_the_event : given.a_single_event_for_event_source_id_command_response_value_handler
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

    Task Because() => _handler.Handle(_commandContext, new EventForEventSourceId(_eventSourceId, _testEvent));

    [Fact] void should_use_the_command_context_routing() => _eventLog.Received(1).Append(
        _eventSourceId,
        _testEvent,
        new EventStreamType("CommandStream"),
        new EventStreamId("command-stream-id"),
        new EventSourceType("Command"),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        new Subject("command-subject"));
}
