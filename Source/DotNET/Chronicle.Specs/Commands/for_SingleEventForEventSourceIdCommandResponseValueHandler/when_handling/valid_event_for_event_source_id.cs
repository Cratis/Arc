// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class valid_event_for_event_source_id : given.a_single_event_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId _value;
    CommandResult _result;
    EventSourceId _eventSourceId;
    TestEvent _testEvent;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _testEvent = new TestEvent("Test Event");
        _value = new EventForEventSourceId(_eventSourceId, _testEvent);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_event_to_event_log() => _eventLog.Received(1).Append(_eventSourceId, _testEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
