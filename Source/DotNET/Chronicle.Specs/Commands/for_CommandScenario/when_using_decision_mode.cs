// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_using_decision_mode
{
    [Fact]
    public async Task absent_decision_can_commit()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        var result = await scenario.Execute(new DecideAtSource(source));
        result.ShouldBeSuccessful();
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeTrue();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task competing_creation_rejects_absent_decision_without_committing_owner_event()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAtSource(source));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        scenario.AppendedEvents.Count.ShouldEqual(0);
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeTrue();
    }

    [Fact]
    public async Task seeded_state_is_read_from_the_same_log()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAtSource(source));
        result.ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task irrelevant_competitor_does_not_conflict()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(EventSourceId.New(), new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAtSource(source));
        result.ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task present_decision_conflicts_when_matching_event_arrives()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAtSource(source));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        scenario.AppendedEvents.Count.ShouldEqual(0);
    }

    [Fact]
    public async Task cross_source_decision_conflicts_even_when_command_appends_to_another_source()
    {
        await using var scenario = new CommandScenario<DecideForAnotherSource>().UseDecisionReads();
        var source = EventSourceId.New();
        var other = EventSourceId.New();
        scenario.AppendConcurrently(other, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideForAnotherSource(source, other));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeFalse();
    }

    [Fact]
    public async Task validate_only_conflicts_without_appending_any_owner_event()
    {
        await using var scenario = new CommandScenario<CheckAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        var result = await scenario.Execute(new CheckAtSource(source));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        scenario.AppendedEvents.Count.ShouldEqual(0);
    }

    [Fact]
    public async Task validate_only_succeeds_if_nothing_changed()
    {
        await using var scenario = new CommandScenario<CheckAtSource>().UseDecisionReads();
        var source = EventSourceId.New();
        (await scenario.Execute(new CheckAtSource(source))).ShouldBeSuccessful();
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeFalse();
    }

    [Fact]
    public async Task explicit_unprotected_command_does_not_enroll_a_guard()
    {
        await using var scenario = new CommandScenario<UnprotectedDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        (await scenario.Execute(new UnprotectedDecision(source))).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task transactional_append_uses_the_decision_store_unit_of_work_manager()
    {
        await using var scenario = new CommandScenario<AppendTransactionallyAfterDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        (await scenario.Execute(new AppendTransactionallyAfterDecision(source))).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeTrue();
    }

    [Fact]
    public async Task nested_command_does_not_drain_competitor_before_outer_read()
    {
        await using var scenario = new CommandScenario<DecideAfterNested>().UseDecisionReads();
        var source = EventSourceId.New();
        var nested = EventSourceId.New();
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAfterNested(source, nested));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        scenario.AppendedEvents.Count.ShouldEqual(0);
        (await scenario.EventLog.HasEventsFor(nested)).ShouldBeFalse();
        (await scenario.EventLog.HasEventsFor(source)).ShouldBeTrue(); // The competitor remains.
    }

    [Fact]
    public async Task protected_nested_command_enrolls_its_own_decision_in_the_outer_transaction()
    {
        await using var scenario = new CommandScenario<DecideAfterProtectedNested>().UseDecisionReads();
        var outer = EventSourceId.New();
        var nested = EventSourceId.New();
        scenario.AppendConcurrently(nested, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideAfterProtectedNested(outer, nested));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task nested_completion_keeps_capturing_outer_commit()
    {
        await using var scenario = new CommandScenario<DecideAfterNested>().UseDecisionReads();
        var source = EventSourceId.New();
        var nested = EventSourceId.New();
        (await scenario.Execute(new DecideAfterNested(source, nested))).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(2);
        scenario.AppendedEvents.Any(_ => _.Event.Context.EventSourceId == source).ShouldBeTrue();
        scenario.AppendedEvents.Any(_ => _.Event.Context.EventSourceId == nested).ShouldBeTrue();
    }

    [Fact]
    public async Task competitor_conflict_compensates_operations_when_owner_did_not_commit()
    {
        await using var scenario = new CommandScenario<DecideWithReservation>().UseDecisionReads();
        var reservations = Substitute.For<IOnboardingReservations>();
        scenario.Services.AddSingleton(reservations);
        var source = EventSourceId.New();
        var reservation = Guid.NewGuid();
        scenario.AppendConcurrently(source, new DecisionStateChanged());
        var result = await scenario.Execute(new DecideWithReservation(source, reservation));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        scenario.ShouldHaveExecutedOperation<ReserveOnboardingCapacity>();
        scenario.ShouldHaveCompensatedOperation<ReserveOnboardingCapacity>();
        result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.NotCommitted);
        await reservations.Received(1).Cancel(reservation, Arg.Any<CancellationToken>());
        scenario.AppendedEvents.Count.ShouldEqual(0);
        var stored = await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, source);
        stored.Count.ShouldEqual(1);
        Assert.IsType<DecisionStateChanged>(stored[0].Content);
    }

    [Fact]
    public void pinned_model_is_refused_in_decision_mode()
    {
        using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        Assert.Throws<NotSupportedException>(() => scenario.Given.ForEventSource(EventSourceId.New()).ReadModel(new DecisionState(Guid.NewGuid())));
    }

    [Fact]
    public void legacy_events_cannot_be_silently_transferred_to_decision_mode()
    {
        using var scenario = new CommandScenario<DecideAtSource>();
        scenario.Given.ForEventSource(EventSourceId.New()).Events(new DecisionStateChanged());
        Assert.Throws<InvalidOperationException>(scenario.UseDecisionReads);
    }

    [Fact]
    public async Task seeded_legacy_event_log_is_refused_before_enabling_decisions()
    {
        await using var scenario = new CommandScenario<DecideAtSource>();
        await scenario.EventLog.Append(EventSourceId.New(), new DecisionStateChanged());
        Assert.Throws<InvalidOperationException>(scenario.UseDecisionReads);
    }

    [Fact]
    public void decision_scenario_does_not_expose_a_second_event_scenario()
    {
        using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        Assert.Throws<NotSupportedException>(() => scenario.EventScenario);
    }

    [Fact]
    public void custom_execution_scope_is_refused_instead_of_silently_dropped()
    {
        using var scenario = new CommandScenario<DecideAtSource>();
        scenario.Services.AddSingleton<ICommandExecutionScope>(new DecisionScenarioConcurrentAppendScope());
        Assert.Throws<NotSupportedException>(scenario.UseDecisionReads);
    }

    [Fact]
    public async Task unmarked_read_is_refused_before_a_returned_event_can_append()
    {
        await using var scenario = new CommandScenario<UnmarkedRead>().UseDecisionReads();
        var result = await scenario.Execute(new UnmarkedRead(EventSourceId.New()));
        Assert.Contains("[ProtectedDecision]", result.ExceptionMessages.Single());
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task unmarked_explicit_key_get_is_refused_before_folding()
    {
        await using var scenario = new CommandScenario<UnmarkedExplicitRead>().UseDecisionReads();
        var result = await scenario.Execute(new UnmarkedExplicitRead(EventSourceId.New()));
        Assert.Contains("[ProtectedDecision]", result.ExceptionMessages.Single());
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task supplied_provider_without_guard_refuses_before_validation_or_handler_work()
    {
        await using var scenario = new CommandScenario<DecideAtSource>().UseDecisionReads();
        scenario.Services.RemoveAll<ICommandProtectedDecisionSupport>();
        var command = new DecideAtSource(EventSourceId.New());
        Assert.Contains("command-aware Chronicle", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("command-aware Chronicle", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Empty(scenario.AppendedEvents);
    }

    [Command]
    public record UnmarkedRead(EventSourceId EventSourceId)
    {
        public DecisionFinished Handle(DecisionRead<DecisionState> read) => new(read.Exists);
    }

    [Command]
    public record UnmarkedExplicitRead(EventSourceId EventSourceId)
    {
        public async Task<DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.Get<DecisionState>((ReadModelKey)EventSourceId)).Exists);
    }

    [Command]
    [ProtectedDecision]
    public record AppendTransactionallyAfterDecision(EventSourceId EventSourceId)
    {
        public async Task Handle(DecisionRead<DecisionState> read, IEventLog log) =>
            await log.Transactional.Append(EventSourceId, new DecisionFinished(read.Exists));
    }

    [Command]
    [ProtectedDecision]
    public record DecideAfterNested(EventSourceId EventSourceId, EventSourceId NestedSource)
    {
        public async Task<DecisionFinished> Handle(ICommandPipeline pipeline, IDecisionReads reads)
        {
            (await pipeline.Execute(new NestedDecisionEvent(NestedSource))).ShouldBeSuccessful();
            return new DecisionFinished((await reads.Get<DecisionState>((ReadModelKey)EventSourceId)).Exists);
        }
    }

    [Command]
    [ProtectedDecision]
    public record DecideAfterProtectedNested(EventSourceId EventSourceId, EventSourceId NestedSource)
    {
        public async Task<DecisionFinished> Handle(ICommandPipeline pipeline, DecisionRead<DecisionState> read)
        {
            (await pipeline.Execute(new ProtectedNestedRead(NestedSource))).ShouldBeSuccessful();
            return new DecisionFinished(read.Exists);
        }
    }

    [Command]
    [ProtectedDecision]
    public record ProtectedNestedRead(EventSourceId EventSourceId)
    {
        public DecisionFinished Handle(DecisionRead<DecisionState> read) => new(read.Exists);
    }

    [Command]
    public record NestedDecisionEvent(EventSourceId EventSourceId)
    {
        public DecisionFinished Handle() => new(false);
    }

    [Command]
    [ProtectedDecision]
    public record DecideWithReservation(EventSourceId EventSourceId, Guid ReservationKey)
    {
        public (DecisionFinished Event, ReserveOnboardingCapacity Operation) Handle(DecisionRead<DecisionState> read) =>
            (new(read.Exists), new(ReservationKey));
    }

    [Command]
    [ProtectedDecision]
    public record DecideForAnotherSource(EventSourceId EventSourceId, EventSourceId Other)
    {
        public async Task<DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.Get<DecisionState>((ReadModelKey)Other)).Exists);
    }

    [Command]
    [ProtectedDecision]
    public record CheckAtSource(EventSourceId EventSourceId)
    {
        public void Handle(DecisionRead<DecisionState> read)
        {
        }
    }

    [Command]
    [Unprotected]
    public record UnprotectedDecision(EventSourceId EventSourceId)
    {
        public DecisionFinished Handle(DecisionRead<DecisionState> read) => new(read.Exists);
    }

    [Command]
    [ProtectedDecision]
    public record DecideAtSource(EventSourceId EventSourceId)
    {
        public object Handle(DecisionRead<DecisionState> state) => new DecisionFinished(state.Exists);
    }

    [FromEvent<DecisionStateChanged>]
    public record DecisionState([property: Key] Guid Id);

    [EventType("3e202e06-a882-45d1-a79e-3edb384f57a7")]
    public record DecisionStateChanged;

    [EventType("1e11560d-c4b9-4847-be45-96873831473d")]
    public record DecisionFinished(bool WasPresent);
}
