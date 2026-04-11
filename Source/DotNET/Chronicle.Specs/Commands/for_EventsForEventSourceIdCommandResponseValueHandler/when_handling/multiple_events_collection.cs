// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class multiple_events_collection : given.an_events_for_event_source_id_command_response_value_handler
{
    IEnumerable<EventForEventSourceId> _value;
    CommandResult _result;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    TestEvent _firstEvent;
    AnotherTestEvent _secondEvent;

    void Establish()
    {
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
        _firstEvent = new TestEvent("First");
        _secondEvent = new AnotherTestEvent(42);
        _value =
        [
            new EventForEventSourceId(_firstEventSourceId, _firstEvent),
            new EventForEventSourceId(_secondEventSourceId, _secondEvent)
        ];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_first_event_with_its_event_source_id() => _eventLog.Received(1).Append(_firstEventSourceId, _firstEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_second_event_with_its_event_source_id() => _eventLog.Received(1).Append(_secondEventSourceId, _secondEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
