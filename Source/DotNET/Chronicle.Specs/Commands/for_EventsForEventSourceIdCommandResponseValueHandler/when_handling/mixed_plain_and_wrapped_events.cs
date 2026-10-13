// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class mixed_plain_and_wrapped_events : given.an_events_for_event_source_id_command_response_value_handler
{
    object[] _value;
    CommandResult _result;
    EventSourceId _commandEventSourceId;
    EventSourceId _wrapperEventSourceId;
    TestEvent _plainEvent;
    AnotherTestEvent _wrappedEvent;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _commandEventSourceId = EventSourceId.New();
        _wrapperEventSourceId = EventSourceId.New();
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceId] = _commandEventSourceId;

        _plainEvent = new TestEvent("Plain");
        _wrappedEvent = new AnotherTestEvent(42);
        _value = [_plainEvent, new EventForEventSourceId(_wrapperEventSourceId, _wrappedEvent)];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_plain_event_to_the_command_event_source_id() => _eventLog.Received(1).Append(_commandEventSourceId, _plainEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_the_wrapped_event_to_its_own_event_source_id() => _eventLog.Received(1).Append(_wrapperEventSourceId, _wrappedEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
}
