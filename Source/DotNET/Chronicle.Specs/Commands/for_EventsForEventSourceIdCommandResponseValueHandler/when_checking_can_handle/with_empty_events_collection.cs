// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_checking_can_handle;

public class with_empty_events_collection : given.an_events_for_event_source_id_command_response_value_handler
{
    IEnumerable<EventForEventSourceId> _value;
    bool _result;

    void Establish() => _value = [];

    void Because() => _result = _handler.CanHandle(_commandContext, _value);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
