// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tuple_with_event_and_subject : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult _result;
    TestEvent _event;
    Subject _subject;

    void Establish()
    {
        _event = new TestEvent("Test");
        _subject = new Subject("customer-42");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns((_event, _subject));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_append_event_to_event_log() => _eventLog.Received(1).Append(
        _command.EventSourceId,
        _event,
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>(),
        Arg.Any<DateTimeOffset?>(),
        _subject);
    [Fact] void should_not_return_subject_as_response() => _result.ShouldBeOfExactType<CommandResult>();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
