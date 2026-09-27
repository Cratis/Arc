// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using FluentValidation;
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
        Assert.Contains("Protected validator dependency", (await scenario.Validate(command)).ExceptionMessages.Single());
        scenario.AppendConcurrently(key, new when_using_decision_mode.DecisionStateChanged());
        Assert.Contains("Protected validator dependency", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, constructions);
        Assert.Equal(0, ScopedCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task factory_and_prebuilt_registrations_with_opaque_dependencies_are_ignored_and_refused_by_shape()
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
        Assert.Contains("Protected validator dependency", (await factoryScenario.Validate(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Protected validator dependency", (await factoryScenario.Execute(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Equal(0, factoryCalls);
        Assert.Empty(factoryScenario.AppendedEvents);

        await using var prebuiltScenario = new CommandScenario<PrebuiltCommand>().UseDecisionReads();
        var prebuilt = new PrebuiltHeldValidator(new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected(
            (ReadModelKey)"old", null)));
        prebuiltScenario.Services.AddSingleton(prebuilt);
        Assert.Contains("Protected validator dependency", (await prebuiltScenario.Validate(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Protected validator dependency", (await prebuiltScenario.Execute(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Empty(prebuiltScenario.AppendedEvents);
    }

    [Fact]
    public async Task factory_and_instance_validators_are_ignored_in_favor_of_fresh_direct_dependencies()
    {
        DirectValidator.Seen.Clear();
        await using var factoryScenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var factoryCalls = 0;
        factoryScenario.Services.AddSingleton<DirectValidator>(_ =>
        {
            factoryCalls++;
            throw new InvalidOperationException("Must not resolve a registered validator factory.");
        });
        (await factoryScenario.Execute(new DirectCommand(EventSourceId.New()))).ShouldBeSuccessful();
        Assert.Equal(0, factoryCalls);
        Assert.Single(DirectValidator.Seen);
        Assert.True(DirectValidator.Seen[0].IsProtected);
        Assert.Single(factoryScenario.AppendedEvents);

        DirectValidator.Seen.Clear();
        await using var instanceScenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var instance = new DirectValidator(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected((ReadModelKey)"old", null));
        DirectValidator.Seen.Clear();
        instanceScenario.Services.AddSingleton(instance);
        (await instanceScenario.Execute(new DirectCommand(EventSourceId.New()))).ShouldBeSuccessful();
        Assert.Single(DirectValidator.Seen);
        Assert.True(DirectValidator.Seen[0].IsProtected);
        Assert.Single(instanceScenario.AppendedEvents);
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
    public async Task nullable_validator_token_resolving_null_cannot_skip_the_guard()
    {
        await using var scenario = new CommandScenario<NullableValidatorCommand>().UseDecisionReads();
        scenario.Services.AddTransient<DecisionRead<when_using_decision_mode.DecisionState>>(_ => null!);
        var command = new NullableValidatorCommand(EventSourceId.New());
        Assert.Contains("directly issued DecisionRead", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("directly issued DecisionRead", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task nullable_handler_and_provide_tokens_resolving_null_cannot_skip_the_guard()
    {
        await using var handlerScenario = new CommandScenario<NullableHandlerCommand>().UseDecisionReads();
        handlerScenario.Services.AddTransient<DecisionRead<when_using_decision_mode.DecisionState>>(_ => null!);
        var key = EventSourceId.New();
        Assert.Contains("directly issued DecisionRead", (await handlerScenario.Execute(new NullableHandlerCommand(key))).ExceptionMessages.Single());
        Assert.Empty(handlerScenario.AppendedEvents);

        await using var provideScenario = new CommandScenario<NullableProvideCommand>().UseDecisionReads();
        provideScenario.Services.AddTransient<DecisionRead<when_using_decision_mode.DecisionState>>(_ => null!);
        Assert.Contains("directly issued DecisionRead", (await provideScenario.Execute(new NullableProvideCommand(key))).ExceptionMessages.Single());
        Assert.Empty(provideScenario.AppendedEvents);
    }

    [Fact]
    public async Task handler_must_not_accept_a_foreign_reader_returned_by_provide()
    {
        await using var scenario = new CommandScenario<ProvidedForeignReaderCommand>().UseDecisionReads();
        var result = await scenario.Execute(new ProvidedForeignReaderCommand(EventSourceId.New()));
        Assert.Contains("command-aware IDecisionReads", result.ExceptionMessages.Single());
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task failed_validator_read_cannot_be_allowed_by_error_severity()
    {
        await using var scenario = new CommandScenario<FailingReadCommand>().UseDecisionReads();
        var reader = Substitute.For<IDecisionReads>();
        reader.GetDetached<when_using_decision_mode.DecisionState>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<when_using_decision_mode.DecisionState>>(new InvalidOperationException("decision acquisition failed")));
        scenario.Services.AddScoped<IDecisionReads>(sp => new CommandDecisionReads(
            reader, sp.GetRequiredService<Cratis.Chronicle.IEventStore>(), sp.GetRequiredService<IReadModels>()));
        var command = new FailingReadCommand(EventSourceId.New());

        // Validation initializes the scenario's registrations; use the same registrations with the pipeline's
        // caller threshold, equivalent to X-Allowed-Severity: Error on an HTTP command request.
        (await scenario.Validate(command)).ExceptionMessages.ShouldNotBeEmpty();
        await using var provider = scenario.Services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ICommandPipeline>();
        var validation = await pipeline.Validate(command, provider, ValidationResultSeverity.Error);
        Assert.Contains("decision acquisition failed", validation.ExceptionMessages.Single());
        var execution = await pipeline.Execute(command, provider, ValidationResultSeverity.Error);
        Assert.Contains("decision acquisition failed", execution.ExceptionMessages.Single());
        Assert.Equal(0, FailingReadCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task http_error_severity_header_cannot_allow_failed_decision_acquisition()
    {
        await using var scenario = new CommandScenario<FailingReadCommand>().UseDecisionReads();
        var reader = Substitute.For<IDecisionReads>();
        reader.GetDetached<when_using_decision_mode.DecisionState>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<when_using_decision_mode.DecisionState>>(new InvalidOperationException("decision acquisition failed")));
        scenario.Services.AddScoped<IDecisionReads>(sp => new CommandDecisionReads(
            reader, sp.GetRequiredService<Cratis.Chronicle.IEventStore>(), sp.GetRequiredService<IReadModels>()));
        var command = new FailingReadCommand(EventSourceId.New());
        (await scenario.Validate(command)).ExceptionMessages.ShouldNotBeEmpty();
        await using var provider = scenario.Services.BuildServiceProvider();
        var mapper = Substitute.For<IEndpointMapper>();
        mapper.MapCommandEndpoints(provider);
        var endpoint = mapper.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEndpointMapper.MapPost) &&
            call.GetArguments().OfType<EndpointMetadata>().Single().Name == $"Execute{typeof(FailingReadCommand).FullName}");
        var handler = (Func<IHttpRequestContext, Task>)endpoint.GetArguments()[1]!;
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(provider);
        context.Headers.Returns(new Dictionary<string, string> { ["X-Allowed-Severity"] = "3" });
        context.ReadBodyAsJson(typeof(FailingReadCommand), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(command));
        CommandResult? response = null;
        context.WriteResponseAsJson(Arg.Do<object?>(value => response = (CommandResult)value!), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await handler(context);

        Assert.NotNull(response);
        Assert.True(response.HasExceptions);
        Assert.False(response.IsSuccess);
        Assert.Equal(0, FailingReadCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
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
    public record NullableValidatorCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class NullableTokenValidator(DecisionRead<when_using_decision_mode.DecisionState>? read) : CommandValidator<NullableValidatorCommand>
    {
        public DecisionRead<when_using_decision_mode.DecisionState>? Read { get; } = read;
    }

    [Command]
    [ProtectedDecision]
    public record NullableHandlerCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle(DecisionRead<when_using_decision_mode.DecisionState>? read) => new(read?.Exists ?? false);
    }

    [Command]
    [ProtectedDecision]
    public record NullableProvideCommand(EventSourceId EventSourceId)
    {
        public DecisionRead<when_using_decision_mode.DecisionState>? Provide(DecisionRead<when_using_decision_mode.DecisionState>? read) => read;
        public when_using_decision_mode.DecisionFinished Handle(DecisionRead<when_using_decision_mode.DecisionState>? read) => new(read?.Exists ?? false);
    }

    [Command]
    [ProtectedDecision]
    public record ProvidedForeignReaderCommand(EventSourceId EventSourceId)
    {
        public IDecisionReads Provide() => Substitute.For<IDecisionReads>();
        public when_using_decision_mode.DecisionFinished Handle(IDecisionReads reads) => new(false);
    }

    [Command]
    [ProtectedDecision]
    public record FailingReadCommand(EventSourceId EventSourceId)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public class FailingReadValidator : CommandValidator<FailingReadCommand>
    {
        public FailingReadValidator(IDecisionReads reads) => RuleFor(_ => _).MustAsync(async (command, token) =>
        {
            await reads.Get<when_using_decision_mode.DecisionState>((ReadModelKey)command.EventSourceId, token);
            return true;
        });
    }

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
