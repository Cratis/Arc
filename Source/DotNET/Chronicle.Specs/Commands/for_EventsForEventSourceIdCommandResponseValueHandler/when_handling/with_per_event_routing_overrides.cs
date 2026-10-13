// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_per_event_routing_overrides : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _overridden;
    EventSourceId _plain;
    TestEvent _testEvent;

    void Establish()
    {
        _overridden = EventSourceId.New();
        _plain = EventSourceId.New();
        _testEvent = new TestEvent("Test Event");
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = new EventSourceType("Command");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = new EventStreamType("CommandStream");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream-id");
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
        new object[]
        {
            new EventForEventSourceId(_overridden, _testEvent)
            {
                EventSourceType = new EventSourceType("Ledger"),
                EventStreamType = new EventStreamType("Postings"),
                EventStreamId = new EventStreamId("postings-1")
            },
            new EventForEventSourceId(_plain, _testEvent)
        });

    [Fact] void should_use_the_routing_set_on_the_event() => (_appended[0].EventStreamType, _appended[0].EventStreamId, _appended[0].EventSourceType).ShouldEqual((new EventStreamType("Postings"), new EventStreamId("postings-1"), new EventSourceType("Ledger")));
    [Fact] void should_use_the_command_context_for_the_event_without_routing() => (_appended[1].EventStreamType, _appended[1].EventStreamId, _appended[1].EventSourceType).ShouldEqual((new EventStreamType("CommandStream"), new EventStreamId("command-stream-id"), new EventSourceType("Command")));
    [Fact] void should_preserve_the_overridden_event_source() => _appended[0].EventSourceId.ShouldEqual(_overridden);
    [Fact] void should_preserve_the_plain_event_source() => _appended[1].EventSourceId.ShouldEqual(_plain);
}
