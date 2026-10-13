// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_enumerable_of_events_for_event_source_id : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult _result;
    IEnumerable<EventForEventSourceId> _value;
    EventForEventSourceId[] _batch;
    EventSourceId _firstTargetEventSourceId;
    EventSourceId _secondTargetEventSourceId;
    TestEvent _firstEvent;
    AnotherTestEvent _secondEvent;

    void Establish()
    {
        _firstTargetEventSourceId = EventSourceId.New();
        _secondTargetEventSourceId = EventSourceId.New();
        _firstEvent = new TestEvent("First");
        _secondEvent = new AnotherTestEvent(42);
        _value =
        [
            new EventForEventSourceId(_firstTargetEventSourceId, _firstEvent),
            new EventForEventSourceId(_secondTargetEventSourceId, _secondEvent),
        ];
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_value);
        _eventLog.AppendMany(default!).ReturnsForAnyArgs(call =>
        {
            _batch = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
            return AppendManyResult.Success(_correlationId, []);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_append_first_event_to_first_target_event_source_id() => _batch[0].EventSourceId.ShouldEqual(_firstTargetEventSourceId);
    [Fact] void should_keep_the_first_event() => _batch[0].Event.ShouldEqual(_firstEvent);
    [Fact] void should_append_second_event_to_second_target_event_source_id() => _batch[1].EventSourceId.ShouldEqual(_secondTargetEventSourceId);
    [Fact] void should_keep_the_second_event() => _batch[1].Event.ShouldEqual(_secondEvent);
    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedWithAnyArgs(1).AppendMany(default!);
}
