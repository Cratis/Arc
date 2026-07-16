// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_checking_can_handle;

public class with_plain_events_only : given.an_events_for_event_source_id_command_response_value_handler
{
    object[] _value;
    bool _result;

    void Establish()
    {
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
        _eventTypes.HasFor(typeof(AnotherTestEvent)).Returns(true);
        _value = [new TestEvent("First"), new AnotherTestEvent(42)];
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _value);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
