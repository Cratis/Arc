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
/// A legacy-stream guard cannot be combined with a guard Chronicle derives from the event source definition for the
/// same id: handing Chronicle a scope would stop it deriving the definition guard, so the command is refused whole.
/// </summary>
public class same_id_events_with_an_implicit_guard_and_a_definition_route : given.an_events_for_event_source_id_command_response_value_handler
{
    IEventSequence _eventSequence;
    UnitOfWork _unitOfWork;
    Exception _exception;
    KeyValuePair<EventSourceId, ConcurrencyScope>[] _scopes = [];
    EventSourceId _id;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _eventLog.Id.Returns(EventSequenceId.Log);
        _commandContext.Values[WellKnownCommandContextKeys.ConcurrencyDimensions] = ConcurrencyDimensions.EventStreamId;
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");

        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>())
            .Returns(call =>
            {
                _scopes = call.Arg<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>().ToArray();
                return Task.FromResult(AppendManyResult.Success(CorrelationId.NotSet, []));
            });
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, eventStore);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        _id = EventSourceId.New();
        _exception = await Catch.Exception(() => _handler.Handle(
            _commandContext,
            new object[]
            {
                new EventForEventSourceId(_id, new TestEvent("first")) { EventStreamId = new EventStreamId("stream-a") },
                new EventForEventSourceId(_id, new TestEvent("second")) { EventSource = typeof(Account), EventStream = "Transactions" }
            }));
        if (_exception is null)
        {
            await _unitOfWork.Commit();
        }
    }

    void Destroy() => CommandTransaction.Current = null;

    [Fact] void should_reject_the_incompatible_guards() => _exception.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_not_stage_any_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_append() => _scopes.ShouldBeEmpty();

    class Account;
}
