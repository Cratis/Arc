// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class empty_events_collection : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;

    void Establish()
    {
        _events = [];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_any_events() => _eventLog.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
