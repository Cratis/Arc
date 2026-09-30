// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

#pragma warning disable SA1402, SA1649

/// <summary>
/// Unmarked and [Unprotected] commands must behave exactly as before protected decisions existed: Chronicle's detached
/// decision reads (shipped since Chronicle 19.13) keep working in their handlers and validators, on both validate and
/// execute. Only [ProtectedDecision] refuses them.
/// </summary>
public class when_an_unmarked_command_reads_a_detached_snapshot
{
    [Fact]
    public async Task handler_reads_the_detached_snapshot_on_validate_and_execute()
    {
        await using var scenario = new CommandScenario<UnmarkedDetachedHandler>();
        var reader = UseDetachedReader(scenario);
        var command = new UnmarkedDetachedHandler(EventSourceId.New());

        (await scenario.Validate(command)).ShouldBeSuccessful();
        (await scenario.Execute(command)).ShouldBeSuccessful();
        await reader.Received(1).GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)command.EventSourceId, Arg.Any<CancellationToken>());
        Assert.Single(scenario.AppendedEvents);
    }

    [Fact]
    public async Task validator_reads_the_detached_snapshot_on_validate_and_execute()
    {
        await using var scenario = new CommandScenario<UnmarkedDetachedValidated>();
        var reader = UseDetachedReader(scenario);
        var command = new UnmarkedDetachedValidated(EventSourceId.New());

        (await scenario.Validate(command)).ShouldBeSuccessful();
        (await scenario.Execute(command)).ShouldBeSuccessful();
        await reader.Received(2).GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)command.EventSourceId, Arg.Any<CancellationToken>());
        Assert.Single(scenario.AppendedEvents);
    }

    [Fact]
    public async Task unprotected_handler_reads_the_detached_snapshot_on_validate_and_execute()
    {
        await using var scenario = new CommandScenario<UnprotectedDetachedHandler>();
        var reader = UseDetachedReader(scenario);
        var command = new UnprotectedDetachedHandler(EventSourceId.New());

        (await scenario.Validate(command)).ShouldBeSuccessful();
        (await scenario.Execute(command)).ShouldBeSuccessful();
        await reader.Received(1).GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)command.EventSourceId, Arg.Any<CancellationToken>());
        Assert.Single(scenario.AppendedEvents);
    }

    [Fact]
    public async Task unprotected_validator_reads_the_detached_snapshot_on_validate_and_execute()
    {
        await using var scenario = new CommandScenario<UnprotectedDetachedValidated>();
        var reader = UseDetachedReader(scenario);
        var command = new UnprotectedDetachedValidated(EventSourceId.New());

        (await scenario.Validate(command)).ShouldBeSuccessful();
        (await scenario.Execute(command)).ShouldBeSuccessful();
        await reader.Received(2).GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)command.EventSourceId, Arg.Any<CancellationToken>());
        Assert.Single(scenario.AppendedEvents);
    }

    [Fact]
    public async Task protected_command_is_still_refused_a_detached_read()
    {
        await using var scenario = new CommandScenario<ProtectedDetachedHandler>().UseDecisionReads();
        var reader = UseDetachedReader(scenario);
        var result = await scenario.Execute(new ProtectedDetachedHandler(EventSourceId.New()));

        Assert.False(result.IsSuccess);
        Assert.Contains("Detached decision reads", result.ExceptionMessages.Single());
        await reader.DidNotReceiveWithAnyArgs().GetDetached<when_using_decision_mode.DecisionState>(default!);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task conflicting_profiles_are_reported_as_a_command_result()
    {
        await using var scenario = new CommandScenario<ConflictingProfiles>();
        var command = new ConflictingProfiles(EventSourceId.New());

        Assert.Contains("cannot be both protected and unprotected", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("cannot be both protected and unprotected", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, ConflictingProfiles.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    static IDecisionReads UseDetachedReader<TCommand>(CommandScenario<TCommand> scenario)
        where TCommand : class
    {
        var reader = Substitute.For<IDecisionReads>();
        reader.GetDetached<when_using_decision_mode.DecisionState>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>())
            .Returns(call => DecisionRead<when_using_decision_mode.DecisionState>.Unprotected(call.Arg<ReadModelKey>(), null));
        scenario.Services.AddScoped<IDecisionReads>(sp => new CommandDecisionReads(
            reader, sp.GetRequiredService<IEventStore>(), sp.GetRequiredService<IReadModels>()));
        return reader;
    }

    [Command]
    public record UnmarkedDetachedHandler(EventSourceId EventSourceId)
    {
        public async Task<when_using_decision_mode.DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)EventSourceId)).Exists);
    }

    [Command]
    public record UnmarkedDetachedValidated(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class UnmarkedDetachedValidatedValidator : CommandValidator<UnmarkedDetachedValidated>
    {
        public UnmarkedDetachedValidatedValidator(IDecisionReads reads) =>
            RuleFor(command => command.EventSourceId)
                .MustAsync(async (id, cancellationToken) =>
                    !(await reads.GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)id, cancellationToken)).Exists)
                .WithMessage("Already decided.");
    }

    [Command]
    [Unprotected]
    public record UnprotectedDetachedHandler(EventSourceId EventSourceId)
    {
        public async Task<when_using_decision_mode.DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)EventSourceId)).Exists);
    }

    [Command]
    [Unprotected]
    public record UnprotectedDetachedValidated(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class UnprotectedDetachedValidatedValidator : CommandValidator<UnprotectedDetachedValidated>
    {
        public UnprotectedDetachedValidatedValidator(IDecisionReads reads) =>
            RuleFor(command => command.EventSourceId)
                .MustAsync(async (id, cancellationToken) =>
                    !(await reads.GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)id, cancellationToken)).Exists)
                .WithMessage("Already decided.");
    }

    [Command]
    [ProtectedDecision]
    public record ProtectedDetachedHandler(EventSourceId EventSourceId)
    {
        public async Task<when_using_decision_mode.DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.GetDetached<when_using_decision_mode.DecisionState>((ReadModelKey)EventSourceId)).Exists);
    }

    [Command]
    [ProtectedDecision]
    [Unprotected]
    public record ConflictingProfiles(EventSourceId EventSourceId)
    {
        public static int Handles;

        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }
}

/// <summary>
/// The harness event log forwards Chronicle's named-tag appends to the in-process log rather than refusing them.
/// </summary>
public class when_an_unmarked_command_appends_with_named_tags
{
    [Fact]
    public async Task named_tag_append_is_forwarded_to_the_in_process_log()
    {
        await using var scenario = new CommandScenario<AppendWithNamedTags>();
        var command = new AppendWithNamedTags(EventSourceId.New());

        (await scenario.Execute(command)).ShouldBeSuccessful();
        (await scenario.EventLog.HasEventsFor(command.EventSourceId)).ShouldBeTrue();
    }

    [Command]
    public record AppendWithNamedTags(EventSourceId EventSourceId)
    {
        public async Task Handle(IEventLog log)
        {
            var result = await log.AppendWithNamedTags(EventSourceId, new when_using_decision_mode.DecisionFinished(true), [new NamedTag("region", "north")]);
            result.IsSuccess.ShouldBeTrue();
        }
    }
}

#pragma warning restore SA1402, SA1649
