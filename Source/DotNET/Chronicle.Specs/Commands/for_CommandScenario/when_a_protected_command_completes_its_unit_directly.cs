// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

#pragma warning disable SA1402, SA1649

public class when_a_protected_command_completes_its_unit_directly
{
    [Fact]
    public async Task rollback_followed_by_an_aggregate_commit_cannot_bypass_the_decision_guard()
    {
        EscapeViaAggregate.Reset();
        await using var scenario = new CommandScenario<EscapeViaAggregate>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(source, new when_using_decision_mode.DecisionStateChanged());

        var result = await scenario.Execute(new EscapeViaAggregate(source));

        Assert.IsType<ProtectedUnitOfWorkRequiresOwner>(EscapeViaAggregate.RollbackFailure);
        Assert.IsType<ProtectedUnitOfWorkRequiresOwner>(EscapeViaAggregate.CommitFailure);
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        Assert.Empty(scenario.AppendedEvents);
        var stored = await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, source);
        Assert.Single(stored);
        Assert.IsType<when_using_decision_mode.DecisionStateChanged>(stored[0].Content);
    }

    [Fact]
    public async Task direct_commit_of_the_unit_is_refused_and_the_owner_still_commits_without_a_conflict()
    {
        EscapeViaAggregate.Reset();
        await using var scenario = new CommandScenario<EscapeViaAggregate>().UseDecisionReads();
        var source = EventSourceId.New();

        var result = await scenario.Execute(new EscapeViaAggregate(source));

        Assert.IsType<ProtectedUnitOfWorkRequiresOwner>(EscapeViaAggregate.RollbackFailure);
        Assert.IsType<ProtectedUnitOfWorkRequiresOwner>(EscapeViaAggregate.CommitFailure);
        result.ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(2);
    }

    [Fact]
    public async Task failed_protected_command_rolls_back_its_unit_as_the_owner()
    {
        FailAfterDecision.Reset();
        await using var scenario = new CommandScenario<FailAfterDecision>().UseDecisionReads();
        var source = EventSourceId.New();

        var result = await scenario.Execute(new FailAfterDecision(source));

        Assert.False(result.IsSuccess);
        Assert.Contains("decision failed", result.ExceptionMessages.Single());
        Assert.NotNull(FailAfterDecision.Unit);
        Assert.True(FailAfterDecision.Unit.HasEnrolledDecisionReads);
        Assert.True(FailAfterDecision.Unit.IsCompleted);
        Assert.Empty(scenario.AppendedEvents);
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeFalse();
    }

    [Command]
    [ProtectedDecision]
    public record EscapeViaAggregate(EventSourceId EventSourceId)
    {
        public static Exception? RollbackFailure { get; private set; }

        public static Exception? CommitFailure { get; private set; }

        public static void Reset()
        {
            RollbackFailure = null;
            CommitFailure = null;
        }

        public async Task<when_using_decision_mode.DecisionFinished> Handle(
            DecisionRead<when_using_decision_mode.DecisionState> read,
            IUnitOfWorkManager unitOfWorkManager,
            IEventSequence eventSequence)
        {
            var unitOfWork = unitOfWorkManager.Current;

            // The direct SDK rollback the owner-claimed unit must refuse, followed by an aggregate root's own commit
            // path: AggregateRootMutation adds its events to the command's unit and commits that unit directly.
            try
            {
                await unitOfWork.Rollback();
            }
            catch (ProtectedUnitOfWorkRequiresOwner failure)
            {
                RollbackFailure = failure;
            }

            var aggregateRoot = new TestAggregateRoot();
            var aggregateSource = EventSourceId.New();
            var context = new AggregateRootContext(
                EventSourceType.Default,
                aggregateSource,
                aggregateRoot.GetEventStreamType(),
                EventStreamId.Default,
                eventSequence,
                aggregateRoot,
                unitOfWork,
                EventSequenceNumber.First,
                EventSequenceNumber.First);
            var mutation = new AggregateRootMutation(context, Substitute.For<IAggregateRootMutator>(), eventSequence);
            await mutation.Apply(new AggregateEventRecorded());
            try
            {
                await mutation.Commit();
            }
            catch (ProtectedUnitOfWorkRequiresOwner failure)
            {
                CommitFailure = failure;
            }

            return new(read.Exists);
        }
    }

    [Command]
    [ProtectedDecision]
    public record FailAfterDecision(EventSourceId EventSourceId)
    {
        public static IUnitOfWork? Unit { get; private set; }

        public static void Reset() => Unit = null;

        public when_using_decision_mode.DecisionFinished Handle(DecisionRead<when_using_decision_mode.DecisionState> read, IUnitOfWorkManager unitOfWorkManager)
        {
            Unit = unitOfWorkManager.Current;
            throw new DecisionFailed();
        }
    }

    public class DecisionFailed() : Exception("decision failed");

    [EventType("a51b0bfa-1d5a-4c9d-8d41-2d0a8b5f7f61")]
    public record AggregateEventRecorded;
}
