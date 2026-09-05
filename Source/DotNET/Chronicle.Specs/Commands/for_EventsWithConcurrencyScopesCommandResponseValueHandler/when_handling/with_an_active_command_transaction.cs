// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_an_active_command_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;
    EventsWithConcurrencyScopes _value;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    EventSourceId _independentLabel;
    FirstEvent _firstEvent;
    SecondEvent _secondEvent;
    ConcurrencyScope _targetScope;
    ConcurrencyScope _independentScope;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;
    Subject _subject;

    void Establish()
    {
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
        _independentLabel = EventSourceId.New();
        _firstEvent = new("first");
        _secondEvent = new(42);
        _targetScope = new(11UL, _firstTarget);
        _independentScope = new(17UL, EventTypes: [new EventType("authority", 1)]);
        _value = new(
            [
                new(_firstTarget, _firstEvent),
                new(_secondTarget, _secondEvent)
            ],
            [
                new(_firstTarget, _targetScope),
                new(_independentLabel, _independentScope)
            ]);

        _eventStreamType = "Membership";
        _eventStreamId = "active";
        _eventSourceType = "Member";
        _subject = "person-42";
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = _eventStreamType;
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = _eventStreamId;
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = _eventSourceType;
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = _subject;
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        _result = await _handler.Handle(_commandContext, _value);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_enroll_one_ordered_batch() => _unitOfWork.AddEventsCallCount.ShouldEqual(1);
    [Fact] void should_use_the_event_log_sequence() => _unitOfWork.AddedEventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_keep_the_first_event_first() => _unitOfWork.AddedEvents[0].Event.ShouldEqual(_firstEvent);
    [Fact] void should_keep_the_first_target_first() => _unitOfWork.AddedEvents[0].EventSourceId.ShouldEqual(_firstTarget);
    [Fact] void should_keep_the_second_event_second() => _unitOfWork.AddedEvents[1].Event.ShouldEqual(_secondEvent);
    [Fact] void should_keep_the_second_target_second() => _unitOfWork.AddedEvents[1].EventSourceId.ShouldEqual(_secondTarget);
    [Fact] void should_preserve_only_the_supplied_scopes() => _unitOfWork.AddedConcurrencyScopes.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_exact_target_scope() => _unitOfWork.AddedConcurrencyScopes.Single(_ => _.Key == _firstTarget).Value.ShouldEqual(_targetScope);
    [Fact] void should_preserve_the_independent_scope_label() => _unitOfWork.AddedConcurrencyScopes.Count(_ => _.Key == _independentLabel).ShouldEqual(1);
    [Fact] void should_preserve_the_independent_scope_sequence_number() => _unitOfWork.AddedConcurrencyScopes.Single(_ => _.Key == _independentLabel).Value.SequenceNumber.ShouldEqual(_independentScope.SequenceNumber);
    [Fact] void should_preserve_the_independent_scope_event_types() => _unitOfWork.AddedConcurrencyScopes.Single(_ => _.Key == _independentLabel).Value.EventTypes.ShouldContainOnly(_independentScope.EventTypes);
    [Fact] void should_apply_command_stream_type() => _unitOfWork.AddedEvents.All(_ => _.EventStreamType == _eventStreamType).ShouldBeTrue();
    [Fact] void should_apply_command_stream_id() => _unitOfWork.AddedEvents.All(_ => _.EventStreamId == _eventStreamId).ShouldBeTrue();
    [Fact] void should_apply_command_event_source_type() => _unitOfWork.AddedEvents.All(_ => _.EventSourceType == _eventSourceType).ShouldBeTrue();
    [Fact] void should_apply_command_subject() => _unitOfWork.AddedEvents.All(_ => _.Subject == _subject).ShouldBeTrue();
    [Fact] void should_apply_command_causation() => _unitOfWork.AddedEvents.All(_ => _.Causation?.Type == CommandTransactionAppender.CausationType).ShouldBeTrue();
    [Fact] void should_put_the_event_sequence_in_causation() => _unitOfWork.AddedEvents.All(_ => _.Causation?.Properties[CommandTransactionAppender.CausationEventSequenceIdProperty] == EventSequenceId.Log.Value).ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _eventLog.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
}
