// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tuple_with_event_first_and_response_second : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult<string> _result;
    (TestEvent, string) _tuple;

    void Establish()
    {
        _tuple = (new TestEvent("Test"), "Response Value");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_append_event_to_event_log() => _eventLog.Received(1).Append(
        _command.EventSourceId,
        _tuple.Item1,
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope>());
    [Fact] void should_return_response_value() => _result.Response.ShouldEqual(_tuple.Item2);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
