// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tuple_with_returned_event_source_id_first_and_event_second : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult<ReturnedEventSourceId> _result;
    (ReturnedEventSourceId, TestEvent) _tuple;

    void Establish()
    {
        _tuple = (new(Guid.NewGuid()), new TestEvent("Test"));
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<ReturnedEventSourceId>;

    [Fact] void should_append_event_to_returned_event_source_id() => _eventLog.Received(1).Append(
        (EventSourceId)_tuple.Item1,
        _tuple.Item2,
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope>());
    [Fact] void should_return_event_source_id() => _result.Response.ShouldEqual(_tuple.Item1);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    record ReturnedEventSourceId(Guid Value) : EventSourceId<Guid>(Value);
}
