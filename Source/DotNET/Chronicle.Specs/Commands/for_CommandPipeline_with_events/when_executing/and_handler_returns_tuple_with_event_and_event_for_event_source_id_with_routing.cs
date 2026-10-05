// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tuple_with_event_and_event_for_event_source_id_with_routing : given.a_command_pipeline_with_event_handlers_and_command
{
    EventSourceId _targetEventSourceId;
    TestEvent _commandEvent;
    AnotherTestEvent _targetEvent;

    void Establish()
    {
        _targetEventSourceId = EventSourceId.New();
        _commandEvent = new TestEvent("On the command source");
        _targetEvent = new AnotherTestEvent(42);
        _commandContextValuesBuilder.Build(_command).Returns(new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, _command.EventSourceId },
            { WellKnownCommandContextKeys.EventSourceType, new EventSourceType("Command") },
            { WellKnownCommandContextKeys.EventStreamType, new EventStreamType("CommandStream") }
        });

        var tuple = (_commandEvent, new EventForEventSourceId(_targetEventSourceId, _targetEvent)
        {
            EventSourceType = new EventSourceType("Ledger"),
            EventStreamType = new EventStreamType("Postings")
        });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(tuple);
    }

    Task Because() => _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_route_the_wrapped_event_with_the_routing_set_on_it() => _eventLog.Received(1).Append(
        _targetEventSourceId,
        _targetEvent,
        new EventStreamType("Postings"),
        Arg.Any<EventStreamId?>(),
        new EventSourceType("Ledger"),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Subject?>());
    [Fact] void should_route_the_plain_event_with_the_command_context() => _eventLog.Received(1).Append(
        _command.EventSourceId,
        _commandEvent,
        new EventStreamType("CommandStream"),
        Arg.Any<EventStreamId?>(),
        new EventSourceType("Command"),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Subject?>());
}
