// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_a_subject_override : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    EventsWithConcurrencyScopes _value;

    void Establish()
    {
        _commandContext = _commandContext with
        {
            Values = new CommandContextValues { { WellKnownCommandContextKeys.Subject, new Subject("command-subject") } }
        };
        _value = new(
            [new(EventSourceId.New(), new FirstEvent("first")) { Subject = new Subject("event-subject") }],
            []);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _handler.Handle(_commandContext, _value);
    }

    [Fact] void should_use_the_subject_set_on_the_event() => _unitOfWork.AddedEvents[0].Subject.ShouldEqual(new Subject("event-subject"));
}
