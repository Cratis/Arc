// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

#pragma warning disable SA1402, SA1649

public class when_protecting_validator_construction
{
    [Fact]
    public async Task scoped_transitive_validator_is_refused_on_validate_and_execute_before_construction_or_append()
    {
        await using var scenario = new CommandScenario<ScopedCommand>().UseDecisionReads();
        var constructions = 0;
        scenario.Services.AddScoped(_ =>
        {
            constructions++;
            return new HeldRead(_.GetRequiredService<DecisionRead<when_using_decision_mode.DecisionState>>());
        });
        scenario.Services.AddScoped<ScopedHeldValidator>();
        var key = EventSourceId.New();
        var command = new ScopedCommand(key);
        Assert.Contains("Registered validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        scenario.AppendConcurrently(key, new when_using_decision_mode.DecisionStateChanged());
        Assert.Contains("Registered validator", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, constructions);
        Assert.Equal(0, ScopedCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task factory_and_prebuilt_registrations_are_refused_before_get_service()
    {
        await using var factoryScenario = new CommandScenario<FactoryCommand>().UseDecisionReads();
        var factoryCalls = 0;
        factoryScenario.Services.AddScoped<FactoryHeldValidator>(_ =>
        {
            factoryCalls++;
            return new FactoryHeldValidator(new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected(
                (ReadModelKey)"old", null)));
        });
        var key = EventSourceId.New();
        Assert.Contains("Registered validator", (await factoryScenario.Validate(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Registered validator", (await factoryScenario.Execute(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Equal(0, factoryCalls);
        Assert.Empty(factoryScenario.AppendedEvents);

        await using var prebuiltScenario = new CommandScenario<PrebuiltCommand>().UseDecisionReads();
        var prebuilt = new PrebuiltHeldValidator(new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected(
            (ReadModelKey)"old", null)));
        prebuiltScenario.Services.AddSingleton(prebuilt);
        Assert.Contains("Registered validator", (await prebuiltScenario.Validate(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Registered validator", (await prebuiltScenario.Execute(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Empty(prebuiltScenario.AppendedEvents);
    }

    [Fact]
    public async Task unregistered_validator_cannot_take_opaque_cached_dependency()
    {
        await using var scenario = new CommandScenario<OpaqueCommand>().UseDecisionReads();
        var dependencies = 0;
        scenario.Services.AddSingleton(_ =>
        {
            dependencies++;
            return new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected((ReadModelKey)"old", null));
        });
        var key = EventSourceId.New();
        Assert.Contains("Protected validator dependency", (await scenario.Validate(new OpaqueCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Protected validator dependency", (await scenario.Execute(new OpaqueCommand(key))).ExceptionMessages.Single());
        Assert.Equal(0, dependencies);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task directly_issued_tokens_are_fresh_between_validation_and_execution()
    {
        DirectValidator.Seen.Clear();
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var key = EventSourceId.New();
        (await scenario.Validate(new DirectCommand(key))).ShouldBeSuccessful();
        (await scenario.Execute(new DirectCommand(key))).ShouldBeSuccessful();
        Assert.Equal(2, DirectValidator.Seen.Count);
        Assert.NotSame(DirectValidator.Seen[0], DirectValidator.Seen[1]);
        Assert.True(DirectValidator.Seen[1].IsProtected);
        Assert.Single(scenario.AppendedEvents);
    }

    [Fact]
    public async Task command_aware_reader_is_supported_directly_in_an_unregistered_validator()
    {
        ReaderValidator.Constructions = 0;
        await using var scenario = new CommandScenario<ReaderCommand>().UseDecisionReads();
        var key = EventSourceId.New();
        (await scenario.Validate(new ReaderCommand(key))).ShouldBeSuccessful();
        (await scenario.Execute(new ReaderCommand(key))).ShouldBeSuccessful();
        Assert.Equal(2, ReaderValidator.Constructions);
        Assert.Single(scenario.AppendedEvents);
    }

    public record HeldRead(DecisionRead<when_using_decision_mode.DecisionState> Read);

    [Command]
    [ProtectedDecision]
    public record ScopedCommand(EventSourceId EventSourceId)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public class ScopedHeldValidator(HeldRead held) : CommandValidator<ScopedCommand>
    {
        public HeldRead Held { get; } = held;
    }

    [Command]
    [ProtectedDecision]
    public record FactoryCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class FactoryHeldValidator(HeldRead held) : CommandValidator<FactoryCommand>
    {
        public HeldRead Held { get; } = held;
    }

    [Command]
    [ProtectedDecision]
    public record PrebuiltCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class PrebuiltHeldValidator(HeldRead held) : CommandValidator<PrebuiltCommand>
    {
        public HeldRead Held { get; } = held;
    }

    [Command]
    [ProtectedDecision]
    public record OpaqueCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class OpaqueHeldValidator(HeldRead held) : CommandValidator<OpaqueCommand>
    {
        public HeldRead Held { get; } = held;
    }

    [Command]
    [ProtectedDecision]
    public record DirectCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class DirectValidator : CommandValidator<DirectCommand>
    {
        public static List<DecisionRead<when_using_decision_mode.DecisionState>> Seen { get; } = [];

        public DirectValidator(DecisionRead<when_using_decision_mode.DecisionState> read) => Seen.Add(read);
    }

    [Command]
    [ProtectedDecision]
    public record ReaderCommand(EventSourceId EventSourceId)
    {
        public async Task<when_using_decision_mode.DecisionFinished> Handle(IDecisionReads reads) =>
            new((await reads.Get<when_using_decision_mode.DecisionState>((ReadModelKey)EventSourceId)).Exists);
    }

    public class ReaderValidator : CommandValidator<ReaderCommand>
    {
        public static int Constructions;

        public ReaderValidator(IDecisionReads reads)
        {
            Assert.NotNull(reads);
            Constructions++;
        }
    }
}

#pragma warning restore SA1402, SA1649
