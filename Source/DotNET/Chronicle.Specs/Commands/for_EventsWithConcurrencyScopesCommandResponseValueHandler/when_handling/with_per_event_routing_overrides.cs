// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_per_event_routing_overrides : given.an_events_with_concurrency_scopes_command_response_value_handler
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
                { WellKnownCommandContextKeys.EventStreamId, new EventStreamId("command-stream-id") }
            }
        };
        _value = new(
            [
                new(EventSourceId.New(), new FirstEvent("first"))
                {
                    EventSourceType = new EventSourceType("Ledger"),
                    EventStreamType = new EventStreamType("Postings")
                },
                new(EventSourceId.New(), new SecondEvent(42))
            ],
            []);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _handler.Handle(_commandContext, _value);
    }

    [Fact] void should_use_the_event_source_type_set_on_the_event() => _unitOfWork.AddedEvents[0].EventSourceType.ShouldEqual(new EventSourceType("Ledger"));
    [Fact] void should_use_the_event_stream_type_set_on_the_event() => _unitOfWork.AddedEvents[0].EventStreamType.ShouldEqual(new EventStreamType("Postings"));
    [Fact] void should_fall_back_to_the_command_stream_id_for_the_unset_value() => _unitOfWork.AddedEvents[0].EventStreamId.ShouldEqual(new EventStreamId("command-stream-id"));
    [Fact] void should_use_the_command_event_source_type_for_the_other_event() => _unitOfWork.AddedEvents[1].EventSourceType.ShouldEqual(new EventSourceType("Command"));
    [Fact] void should_use_the_command_event_stream_type_for_the_other_event() => _unitOfWork.AddedEvents[1].EventStreamType.ShouldEqual(new EventStreamType("CommandStream"));
}
