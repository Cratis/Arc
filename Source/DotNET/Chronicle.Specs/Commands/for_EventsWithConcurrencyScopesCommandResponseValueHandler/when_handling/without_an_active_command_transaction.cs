// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class without_an_active_command_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;
    EventsWithConcurrencyScopes _value;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    EventSourceId _independentLabel;
    FirstEvent _firstEvent;
    SecondEvent _secondEvent;
    ConcurrencyScope _independentScope;
    EventForEventSourceId[] _appendedEvents;
    IDictionary<EventSourceId, ConcurrencyScope> _appendedScopes;

    void Establish()
    {
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
        _independentLabel = EventSourceId.New();
        _firstEvent = new("first");
        _secondEvent = new(42);
        _independentScope = new(17UL, EventTypes: [new EventType("authority", 1)]);
        _value = new(
            [
                new(_firstTarget, _firstEvent),
                new(_secondTarget, _secondEvent)
            ],
            [new(_independentLabel, _independentScope)]);

        _eventLog
            .AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                Arg.Any<CorrelationId?>(),
                Arg.Any<IEnumerable<string>?>(),
                Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(callInfo =>
            {
                _appendedEvents = callInfo.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                _appendedScopes = callInfo.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                return AppendManyResult.Success(_correlationId, [EventSequenceNumber.First, new EventSequenceNumber(1)]);
            });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_one_ordered_batch() => _eventLog.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
    [Fact] void should_keep_the_first_event_first() => _appendedEvents[0].Event.ShouldEqual(_firstEvent);
    [Fact] void should_keep_the_first_target_first() => _appendedEvents[0].EventSourceId.ShouldEqual(_firstTarget);
    [Fact] void should_keep_the_second_event_second() => _appendedEvents[1].Event.ShouldEqual(_secondEvent);
    [Fact] void should_keep_the_second_target_second() => _appendedEvents[1].EventSourceId.ShouldEqual(_secondTarget);
    [Fact] void should_preserve_the_independent_scope_sequence_number() => _appendedScopes[_independentLabel].SequenceNumber.ShouldEqual(_independentScope.SequenceNumber);
    [Fact] void should_preserve_the_independent_scope_event_types() => _appendedScopes[_independentLabel].EventTypes.ShouldContainOnly(_independentScope.EventTypes);
    [Fact] void should_not_enroll_in_a_unit_of_work() => _unitOfWork.AddEventsCallCount.ShouldEqual(0);
}
