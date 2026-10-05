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
/// Events written through a definition and carrying a definition-derived default must reach the event sequence with no
/// scope attached, so the sequence derives and validates each event's own guard. A scope built from the command's own
/// stream and attached for the id would count as the caller's explicit choice and bypass that validation.
/// </summary>
public class same_id_events_in_different_definition_streams : given.an_events_for_event_source_id_command_response_value_handler
{
    IEventSequence _eventSequence;
    UnitOfWork _unitOfWork;
    IDictionary<EventSourceId, ConcurrencyScope> _scopes;
    int _appendedEvents;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _eventLog.Id.Returns(EventSequenceId.Log);
        _commandContext.Values[WellKnownCommandContextKeys.EventSource] = typeof(AccountSource);
        _commandContext.Values[WellKnownCommandContextKeys.EventStream] = "Transactions";
        _commandContext.Values[WellKnownCommandContextKeys.ConcurrencyDimensions] = ConcurrencyDimensions.EventStreamId;
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");

        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                _appendedEvents = call.Arg<IEnumerable<EventForEventSourceId>>().Count();
                _scopes = new Dictionary<EventSourceId, ConcurrencyScope>(call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>());
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
        await _handler.Handle(
            _commandContext,
            new object[]
            {
                new EventForEventSourceId(id, new TestEvent("first")) { EventStream = "Transactions", EventStreamId = new EventStreamId("stream-a") },
                new EventForEventSourceId(id, new TestEvent("second")) { EventStream = "Postings", EventStreamId = new EventStreamId("stream-b") }
            });
        await _unitOfWork.Commit();
    }

    void Destroy() => CommandTransaction.Current = null;

    [Fact] void should_commit_both_events() => _appendedEvents.ShouldEqual(2);
    [Fact] void should_not_attach_a_framework_derived_scope() => _scopes.Values.ShouldEachConformTo(_ => _ == ConcurrencyScope.NotSet);
    [Fact] void should_not_ask_the_strategy_for_a_command_level_scope() => _concurrencyScopeStrategy.DidNotReceiveWithAnyArgs().GetScope(default!, default, default, default, default);

    class AccountSource;
}
