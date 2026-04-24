// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_event_for_event_source_id : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult _result;
    EventForEventSourceId _value;
    EventSourceId _targetEventSourceId;
    TestEvent _testEvent;

    void Establish()
    {
        _targetEventSourceId = EventSourceId.New();
        _testEvent = new TestEvent("From EventForEventSourceId");
        _value = new EventForEventSourceId(_targetEventSourceId, _testEvent);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_value);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_append_event_to_target_event_source_id() => _eventLog.Received(1).Append(
        _targetEventSourceId,
        _testEvent,
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope>());
    [Fact] void should_not_append_to_command_event_source_id() => _eventLog.DidNotReceive().Append(
        _command.EventSourceId,
        _testEvent,
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope>());
}
