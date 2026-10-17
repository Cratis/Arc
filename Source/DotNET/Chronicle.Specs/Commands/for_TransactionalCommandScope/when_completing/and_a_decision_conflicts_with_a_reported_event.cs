// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_a_decision_conflicts_with_a_reported_event : given.a_transactional_command_scope
{
    static readonly EventTypeId _eventTypeId = new("8f1e9c1a-book-borrowed");
    CommandResult _result;

    void Establish()
    {
        var (store, log, unit) = DecisionFixtures.Transaction();
        _unitOfWork = unit;
        _unitOfWorkManager.Begin(Arg.Any<Cratis.Execution.CorrelationId>()).Returns(unit);
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.HasFor(_eventTypeId).Returns(true);
        eventTypes.GetClrTypeFor(_eventTypeId).Returns(typeof(BookBorrowed));
        store.EventTypes.Returns(eventTypes);
        _serviceProvider.GetService(typeof(IEventStore)).Returns(store);
        log.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<Cratis.Execution.CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(AppendManyResult.Failed(_correlationId, [
                new ConcurrencyViolation("source", (EventSequenceNumber)2ul, (EventSequenceNumber)3ul)
                {
                    ConflictingEventTypeId = _eventTypeId,
                    ConflictingEventSequenceNumber = (EventSequenceNumber)1042ul
                }]));
    }

    async Task Because()
    {
        _result = CommandResult.Success(_correlationId);
        _scope.Begin(_context);
        _unitOfWork.AddDecisionRead(DecisionFixtures.Protected<SampleModel>("source"));
        await _scope.Complete(_context, _result);
    }

    [Fact]
    void should_name_the_conflicting_event_and_its_position() =>
        _result.ValidationResults.Single(_ => _.Message.Contains("'source' changed")).Message
            .ShouldEqual("SampleModel 'source' changed after it was read (BookBorrowed at 1042). Read it again and resubmit.");

    [Fact]
    void should_carry_the_conflicting_event_in_the_state()
    {
        var state = JsonSerializer.SerializeToElement(_result.ValidationResults.Single(_ => _.Message.Contains("'source' changed")).State);
        state.GetProperty("readModel").GetString().ShouldEqual(nameof(SampleModel));
        state.GetProperty("key").GetString().ShouldEqual("source");
        state.GetProperty("conflictingEventTypeId").GetString().ShouldEqual(_eventTypeId.Value);
        state.GetProperty("conflictingEventType").GetString().ShouldEqual(nameof(BookBorrowed));
        state.GetProperty("conflictingEventSequenceNumber").GetUInt64().ShouldEqual(1042ul);
    }

    public class SampleModel;

    public record BookBorrowed;
}
