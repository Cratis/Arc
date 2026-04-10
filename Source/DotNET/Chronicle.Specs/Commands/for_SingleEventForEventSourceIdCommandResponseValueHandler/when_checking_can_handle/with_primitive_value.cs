// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_checking_can_handle;

public class with_primitive_value : given.a_single_event_for_event_source_id_command_response_value_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_commandContext, "not an event");

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
