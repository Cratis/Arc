// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_enumerable_of_events_for_event_source_id : given.a_command_pipeline_with_event_handlers_and_command
{
    CommandResult _result;
    IEnumerable<EventForEventSourceId> _value;
    EventForEventSourceId[] _appended;
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
        _appended = [];
        _eventLog.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
            return AppendManyResult.Success(_correlationId, []);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_append_first_event_to_first_target_event_source_id() => (_appended[0].EventSourceId, _appended[0].Event).ShouldEqual((_firstTargetEventSourceId, _firstEvent));
    [Fact] void should_append_second_event_to_second_target_event_source_id() => (_appended[1].EventSourceId, _appended[1].Event).ShouldEqual((_secondTargetEventSourceId, _secondEvent));
    [Fact] void should_append_both_events() => _appended.Length.ShouldEqual(2);
    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
