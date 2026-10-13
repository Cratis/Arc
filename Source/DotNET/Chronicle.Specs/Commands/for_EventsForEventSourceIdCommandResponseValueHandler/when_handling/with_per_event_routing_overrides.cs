// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

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
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = new Subject("command-subject");
    }

    Task Because() => _handler.Handle(
        _commandContext,
        new object[]
        {
            new EventForEventSourceId(_overridden, _testEvent)
            {
                EventSourceType = new EventSourceType("Ledger"),
                EventStreamType = new EventStreamType("Postings"),
                EventStreamId = new EventStreamId("postings-1"),
                Subject = new Subject("wrapper-subject")
            },
            new EventForEventSourceId(_plain, _testEvent)
        });

    [Fact] void should_keep_the_overridden_source_id() => _batch[0].EventSourceId.ShouldEqual(_overridden);
    [Fact] void should_keep_the_overridden_source_type() => _batch[0].EventSourceType.ShouldEqual(new EventSourceType("Ledger"));
    [Fact] void should_keep_the_overridden_stream_type() => _batch[0].EventStreamType.ShouldEqual(new EventStreamType("Postings"));
    [Fact] void should_keep_the_overridden_stream_id() => _batch[0].EventStreamId.ShouldEqual(new EventStreamId("postings-1"));
    [Fact] void should_keep_the_overridden_subject() => _batch[0].Subject.ShouldEqual(new Subject("wrapper-subject"));
    [Fact] void should_keep_the_other_source_id() => _batch[1].EventSourceId.ShouldEqual(_plain);
    [Fact] void should_use_the_command_source_type() => _batch[1].EventSourceType.ShouldEqual(new EventSourceType("Command"));
    [Fact] void should_use_the_command_stream_type() => _batch[1].EventStreamType.ShouldEqual(new EventStreamType("CommandStream"));
    [Fact] void should_use_the_command_stream_id() => _batch[1].EventStreamId.ShouldEqual(new EventStreamId("command-stream-id"));
    [Fact] void should_use_the_command_subject() => _batch[1].Subject.ShouldEqual(new Subject("command-subject"));
}
