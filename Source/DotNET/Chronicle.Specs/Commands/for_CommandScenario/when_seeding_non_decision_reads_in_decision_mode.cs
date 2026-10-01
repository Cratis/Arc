// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_seeding_non_decision_reads_in_decision_mode
{
    [Fact]
    public async Task injected_read_model_materializes_from_the_seeded_events()
    {
        await using var scenario = new CommandScenario<ReadAdvisoryAlongsideDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new GuardedStateChanged("guarded"), new AdvisoryNoted("seeded"));
        (await scenario.Execute(new ReadAdvisoryAlongsideDecision(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadAdvisoryAlongsideDecision, AdvisoryObserved>(source, _ => _.DecisionExists && _.Note == "seeded");
    }

    [Fact]
    public async Task read_models_resolve_from_the_seeded_events()
    {
        await using var scenario = new CommandScenario<ReadAdvisoryThroughReadModels>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new AdvisoryNoted("seeded"));
        (await scenario.Execute(new ReadAdvisoryThroughReadModels(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadAdvisoryThroughReadModels, AdvisoryObserved>(source, _ => !_.DecisionExists && _.Note == "seeded");
    }

    [Fact]
    public async Task unprotected_decision_read_materializes_from_the_seeded_events()
    {
        await using var scenario = new CommandScenario<ReadUnprotected>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new AdvisoryNoted("seeded"));
        (await scenario.Execute(new ReadUnprotected(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadUnprotected, AdvisoryObserved>(source, _ => _.Note == "seeded");
    }

    [Fact]
    public async Task pinned_read_model_serves_an_injected_read_model()
    {
        await using var scenario = new CommandScenario<ReadAdvisoryAlongsideDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).ReadModel(new AdvisoryState(Guid.Parse(source.Value), "pinned"));
        (await scenario.Execute(new ReadAdvisoryAlongsideDecision(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadAdvisoryAlongsideDecision, AdvisoryObserved>(source, _ => !_.DecisionExists && _.Note == "pinned");
    }

    [Fact]
    public async Task pinned_read_model_serves_an_unprotected_decision_read()
    {
        await using var scenario = new CommandScenario<ReadUnprotected>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).ReadModel(new AdvisoryState(Guid.Parse(source.Value), "pinned"));
        (await scenario.Execute(new ReadUnprotected(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadUnprotected, AdvisoryObserved>(source, _ => _.Note == "pinned");
    }

    [Fact]
    public async Task competing_append_to_a_non_decision_read_neither_shows_up_nor_conflicts()
    {
        await using var scenario = new CommandScenario<ReadAdvisoryAlongsideDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.AppendConcurrently(source, new AdvisoryNoted("late"));
        (await scenario.Execute(new ReadAdvisoryAlongsideDecision(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadAdvisoryAlongsideDecision, AdvisoryObserved>(source, _ => _.Note.Length == 0);
    }

    [Fact]
    public void pinning_a_protected_decision_read_names_the_command_and_read_model()
    {
        using var scenario = new CommandScenario<ReadAdvisoryAlongsideDecision>().UseDecisionReads();
        var source = EventSourceId.New();
        var error = Assert.Throws<PinnedReadModelCannotProvideDecisionToken>(() => scenario.Given.ForEventSource(source).ReadModel(new GuardedState(Guid.Parse(source.Value), "pinned")));
        Assert.Contains($"'{nameof(ReadAdvisoryAlongsideDecision)}' reads '{nameof(GuardedState)}'", error.Message);
    }

    [Command]
    [ProtectedDecision]
    public record ReadAdvisoryAlongsideDecision(EventSourceId EventSourceId)
    {
        public AdvisoryObserved Handle(DecisionRead<GuardedState> decision, AdvisoryState? advisory) => new(decision.Exists, advisory?.Note ?? string.Empty);
    }

    [Command]
    [ProtectedDecision]
    public record ReadAdvisoryThroughReadModels(EventSourceId EventSourceId)
    {
        public async Task<AdvisoryObserved> Handle(DecisionRead<GuardedState> decision, IReadModels readModels) =>
            new(decision.Exists, (await readModels.GetInstanceById<AdvisoryState>(EventSourceId.Value))?.Note ?? string.Empty);
    }

    [Command]
    [Unprotected]
    public record ReadUnprotected(EventSourceId EventSourceId)
    {
        public AdvisoryObserved Handle(DecisionRead<AdvisoryState> advisory) => new(false, advisory.Instance?.Note ?? string.Empty);
    }

    [FromEvent<GuardedStateChanged>]
    public record GuardedState([property: Key] Guid Id, string Status);

    [FromEvent<AdvisoryNoted>]
    public record AdvisoryState([property: Key] Guid Id, string Note);

    [EventType("85e16472-9413-4ab2-bc32-00a69ddcba1f")]
    public record GuardedStateChanged(string Status);

    [EventType("573ac127-6f01-43c1-9a0d-deffed8d887f")]
    public record AdvisoryNoted(string Note);

    [EventType("3f367350-f0a4-46eb-b451-1d11ccfac065")]
    public record AdvisoryObserved(bool DecisionExists, string Note);
}
