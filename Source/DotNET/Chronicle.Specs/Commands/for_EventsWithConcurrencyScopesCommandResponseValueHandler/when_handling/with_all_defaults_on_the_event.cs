// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_all_defaults_on_the_event : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    EventsWithConcurrencyScopes _value;

    void Establish()
    {
        _commandContext = _commandContext with
        {
            Values = new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventSourceType, new EventSourceType("Command") },
                { WellKnownCommandContextKeys.EventStreamType, new EventStreamType("CommandStream") },
                { WellKnownCommandContextKeys.EventStreamId, new EventStreamId("command-stream-id") },
                { WellKnownCommandContextKeys.Subject, new Subject("command-subject") }
            }
        };
        _value = new([new(EventSourceId.New(), new FirstEvent("first"))], []);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _handler.Handle(_commandContext, _value);
    }

    [Fact] void should_use_the_command_event_source_type() => _unitOfWork.AddedEvents[0].EventSourceType.ShouldEqual(new EventSourceType("Command"));
    [Fact] void should_use_the_command_event_stream_type() => _unitOfWork.AddedEvents[0].EventStreamType.ShouldEqual(new EventStreamType("CommandStream"));
    [Fact] void should_use_the_command_event_stream_id() => _unitOfWork.AddedEvents[0].EventStreamId.ShouldEqual(new EventStreamId("command-stream-id"));
    [Fact] void should_use_the_command_subject() => _unitOfWork.AddedEvents[0].Subject.ShouldEqual(new Subject("command-subject"));
}
