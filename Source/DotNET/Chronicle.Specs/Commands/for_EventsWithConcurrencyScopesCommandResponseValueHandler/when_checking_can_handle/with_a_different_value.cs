// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_checking_can_handle;

public class with_a_different_value : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_commandContext, new object());

    [Fact] void should_not_be_able_to_handle() => _result.ShouldBeFalse();
}
