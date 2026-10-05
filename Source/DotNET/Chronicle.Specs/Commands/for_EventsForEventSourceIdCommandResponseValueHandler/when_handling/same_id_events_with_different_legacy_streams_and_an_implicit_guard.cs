// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

/// <summary>
/// A guard derived from the definition is a default, not a choice: two events for one id that would need different
/// guards fail before anything reaches the unit of work, instead of the first guard silently covering both.
/// </summary>
public class same_id_events_with_different_legacy_streams_and_an_implicit_guard : given.an_events_for_event_source_id_command_response_value_handler
{
    IEventSequence _eventSequence;
    UnitOfWork _unitOfWork;
    Exception _exception;
    bool _appended;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _eventLog.Id.Returns(EventSequenceId.Log);
        _commandContext.Values[WellKnownCommandContextKeys.ConcurrencyDimensions] = ConcurrencyDimensions.EventStreamId;
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");

        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(_ =>
            {
                _appended = true;
                return Task.FromResult(AppendManyResult.Success(CorrelationId.NotSet, []));
            });
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, eventStore);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        var id = EventSourceId.New();
        _exception = await Catch.Exception(() => _handler.Handle(
            _commandContext,
            new object[]
            {
                new EventForEventSourceId(id, new TestEvent("first")) { EventStreamId = new EventStreamId("stream-a") },
                new EventForEventSourceId(id, new TestEvent("second")) { EventStreamId = new EventStreamId("stream-b") }
            }));
        if (_exception is null)
        {
            await _unitOfWork.Commit();
        }
    }

    void Destroy() => CommandTransaction.Current = null;

    [Fact] void should_reject_the_incompatible_guards() => _exception.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_not_stage_any_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_append() => _appended.ShouldBeFalse();
}
