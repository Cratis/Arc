// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_checking_can_handle;

public class with_a_null_element_in_the_collection : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    bool _result;

    void Establish()
    {
        _events = [new TestEvent("Test"), null!];
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _events);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
