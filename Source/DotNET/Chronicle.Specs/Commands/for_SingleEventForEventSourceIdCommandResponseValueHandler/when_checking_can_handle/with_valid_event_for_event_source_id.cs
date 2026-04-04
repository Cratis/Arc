// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_checking_can_handle;

public class with_valid_event_for_event_source_id : given.a_single_event_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId _value;
    bool _result;

    void Establish()
    {
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _value = new EventForEventSourceId(EventSourceId.New(), new TestEvent("Test Event"));
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _value);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
