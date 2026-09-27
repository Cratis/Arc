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
using Cratis.Concepts;
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
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        scenario.AppendConcurrently(key, new when_using_decision_mode.DecisionStateChanged());
        Assert.Contains("Discoverable validator", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, constructions);
        Assert.Equal(0, ScopedCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task factory_and_prebuilt_registrations_are_refused_before_resolving_dependencies()
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
        Assert.Contains("Discoverable validator", (await factoryScenario.Validate(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await factoryScenario.Execute(new FactoryCommand(key))).ExceptionMessages.Single());
        Assert.Equal(0, factoryCalls);
        Assert.Empty(factoryScenario.AppendedEvents);

        await using var prebuiltScenario = new CommandScenario<PrebuiltCommand>().UseDecisionReads();
        var prebuilt = new PrebuiltHeldValidator(new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected(
            (ReadModelKey)"old", null)));
        prebuiltScenario.Services.AddSingleton(prebuilt);
        Assert.Contains("Discoverable validator", (await prebuiltScenario.Validate(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await prebuiltScenario.Execute(new PrebuiltCommand(key))).ExceptionMessages.Single());
        Assert.Empty(prebuiltScenario.AppendedEvents);
    }

    [Fact]
    public async Task factory_added_ownership_rule_cannot_disappear_in_a_protected_command()
    {
        await using var scenario = new CommandScenario<OwnershipCommand>().UseDecisionReads();
        var factories = 0;
        scenario.Services.AddSingleton<OwnershipValidator>(_ =>
        {
            factories++;
            var validator = new OwnershipValidator();
            validator.RuleFor(command => command.EventSourceId).Must(_ => false).WithMessage("ownership denied");
            return validator;
        });
        var result = await scenario.Execute(new OwnershipCommand(EventSourceId.New()));
        Assert.Contains("Discoverable validator", result.ExceptionMessages.Single());
        Assert.Equal(0, factories);
        Assert.Equal(0, OwnershipCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task legacy_factory_added_ownership_rule_still_rejects_the_command()
    {
        await using var scenario = new CommandScenario<LegacyOwnershipCommand>();
        var factories = 0;
        scenario.Services.AddSingleton<LegacyOwnershipValidator>(_ =>
        {
            factories++;
            var validator = new LegacyOwnershipValidator();
            validator.RuleFor(command => command.EventSourceId).Must(_ => false).WithMessage("ownership denied");
            return validator;
        });
        var result = await scenario.Execute(new LegacyOwnershipCommand(EventSourceId.New()));
        Assert.Contains(result.ValidationResults, _ => _.Message == "ownership denied");
        Assert.Equal(1, factories);
        Assert.Equal(0, LegacyOwnershipCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task explicit_validator_instance_with_a_direct_token_constructor_is_refused()
    {
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        scenario.Services.AddSingleton(new DirectValidator(
            DecisionRead<when_using_decision_mode.DecisionState>.Unprotected((ReadModelKey)"old", null)));
        DirectValidator.Seen.Clear();
        Assert.Contains("Discoverable validator", (await scenario.Execute(new DirectCommand(EventSourceId.New()))).ExceptionMessages.Single());
        Assert.Empty(DirectValidator.Seen);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task explicit_registration_after_arc_conventions_is_also_refused()
    {
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        Assert.Contains("Discoverable validator", (await scenario.Validate(new DirectCommand(EventSourceId.New()))).ExceptionMessages.Single());
        var factories = 0;
        scenario.Services.AddTransient<DirectValidator>(_ =>
        {
            factories++;
            throw new InvalidOperationException("Application factory must not run.");
        });
        await using var provider = scenario.Services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ICommandPipeline>();
        Assert.Contains("Discoverable validator", (await pipeline.Execute(new DirectCommand(EventSourceId.New()), provider)).ExceptionMessages.Single());
        Assert.Equal(0, factories);
        Assert.Equal(0, DirectCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task duplicate_validator_bindings_are_refused_before_any_factory_runs()
    {
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var factories = 0;
        scenario.Services.AddTransient<DirectValidator>(_ =>
        {
            factories++;
            throw new InvalidOperationException("First factory must not run.");
        });
        scenario.Services.AddScoped<DirectValidator>(_ =>
        {
            factories++;
            throw new InvalidOperationException("Second factory must not run.");
        });
        Assert.Contains("Discoverable validator", (await scenario.Execute(new DirectCommand(EventSourceId.New()))).ExceptionMessages.Single());
        Assert.Equal(0, factories);
    }

    [Fact]
    public async Task cloned_provider_with_factory_override_refuses_from_original_and_cloned_pipelines()
    {
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var command = new DirectCommand(EventSourceId.New());
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        var clonedServices = new ServiceCollection();
        foreach (var descriptor in scenario.Services) ((IServiceCollection)clonedServices).Add(descriptor);
        var factories = 0;
        clonedServices.AddTransient<DirectValidator>(_ =>
        {
            factories++;
            throw new InvalidOperationException("Cloned factory must not run.");
        });
        await using var clone = clonedServices.BuildServiceProvider();
        await using var original = scenario.Services.BuildServiceProvider();
        foreach (var pipeline in new[] { original.GetRequiredService<ICommandPipeline>(), clone.GetRequiredService<ICommandPipeline>() })
        {
            Assert.Contains("Discoverable validator", (await pipeline.Validate(command, clone, ValidationResultSeverity.Error)).ExceptionMessages.Single());
            Assert.Contains("Discoverable validator", (await pipeline.Execute(command, clone, ValidationResultSeverity.Error)).ExceptionMessages.Single());
        }
        Assert.Equal(0, factories);
        Assert.Equal(0, DirectCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task post_build_collection_mutation_cannot_enable_protected_validator()
    {
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        Assert.Contains("Discoverable validator", (await scenario.Validate(new DirectCommand(EventSourceId.New()))).ExceptionMessages.Single());
        await using var provider = scenario.Services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ICommandPipeline>();
        var factories = 0;
        scenario.Services.AddTransient<DirectValidator>(_ =>
        {
            factories++;
            throw new InvalidOperationException("Post-build factory must not run.");
        });
        var command = new DirectCommand(EventSourceId.New());
        Assert.Contains("Discoverable validator", (await pipeline.Validate(command, provider, ValidationResultSeverity.Error)).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await pipeline.Execute(command, provider, ValidationResultSeverity.Error)).ExceptionMessages.Single());
        Assert.Equal(0, factories);
        Assert.Equal(0, DirectCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task nested_concept_validator_is_refused_before_constructing_or_running_rules()
    {
        await using var scenario = new CommandScenario<NestedConceptCommand>().UseDecisionReads();
        NestedConceptValidator.Constructions = 0;
        var command = new NestedConceptCommand(EventSourceId.New(), new NestedConcept("value"));
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, NestedConceptValidator.Constructions);
        Assert.Equal(0, NestedConceptCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task validator_with_opaque_cached_dependency_is_refused_before_resolving_it()
    {
        await using var scenario = new CommandScenario<OpaqueCommand>().UseDecisionReads();
        var dependencies = 0;
        scenario.Services.AddSingleton(_ =>
        {
            dependencies++;
            return new HeldRead(DecisionRead<when_using_decision_mode.DecisionState>.Unprotected((ReadModelKey)"old", null));
        });
        var key = EventSourceId.New();
        Assert.Contains("Discoverable validator", (await scenario.Validate(new OpaqueCommand(key))).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await scenario.Execute(new OpaqueCommand(key))).ExceptionMessages.Single());
        Assert.Equal(0, dependencies);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task convention_self_bound_validator_is_refused_without_constructing_or_folding_a_direct_token()
    {
        DirectValidator.Seen.Clear();
        await using var scenario = new CommandScenario<DirectCommand>().UseDecisionReads();
        var command = new DirectCommand(EventSourceId.New());
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains(scenario.Services, descriptor => descriptor.ServiceType == typeof(DirectValidator) &&
            descriptor.ImplementationType == typeof(DirectValidator));
        Assert.Contains("Discoverable validator", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Empty(DirectValidator.Seen);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task nullable_validator_token_is_refused_before_resolution()
    {
        await using var scenario = new CommandScenario<NullableValidatorCommand>().UseDecisionReads();
        scenario.Services.AddTransient<DecisionRead<when_using_decision_mode.DecisionState>>(_ => null!);
        var command = new NullableValidatorCommand(EventSourceId.New());
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await scenario.Execute(command)).ExceptionMessages.Single());
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
    public async Task parameterless_validator_is_refused_before_running_its_rules()
    {
        await using var scenario = new CommandScenario<NullConceptCommand>().UseDecisionReads();
        var result = await scenario.Execute(new NullConceptCommand(EventSourceId.New(), null));
        Assert.Contains("Discoverable validator", result.ExceptionMessages.Single());
        Assert.Empty(result.ValidationResults);
        Assert.Equal(0, NullConceptCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
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
    public async Task failed_handler_read_cannot_be_allowed_by_error_severity()
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
        (await scenario.Validate(command)).ShouldBeSuccessful();
        await using var provider = scenario.Services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ICommandPipeline>();
        (await pipeline.Validate(command, provider, ValidationResultSeverity.Error)).ShouldBeSuccessful();
        var execution = await pipeline.Execute(command, provider, ValidationResultSeverity.Error);
        Assert.Contains("decision acquisition failed", execution.ExceptionMessages.Single());
        Assert.Equal(0, FailingReadCommand.Handles);
        Assert.Empty(scenario.AppendedEvents);
    }

    [Fact]
    public async Task http_error_severity_header_cannot_allow_failed_handler_read()
    {
        await using var scenario = new CommandScenario<FailingReadCommand>().UseDecisionReads();
        var reader = Substitute.For<IDecisionReads>();
        reader.GetDetached<when_using_decision_mode.DecisionState>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<when_using_decision_mode.DecisionState>>(new InvalidOperationException("decision acquisition failed")));
        scenario.Services.AddScoped<IDecisionReads>(sp => new CommandDecisionReads(
            reader, sp.GetRequiredService<Cratis.Chronicle.IEventStore>(), sp.GetRequiredService<IReadModels>()));
        var command = new FailingReadCommand(EventSourceId.New());
        (await scenario.Validate(command)).ShouldBeSuccessful();
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
    public async Task command_aware_reader_in_a_validator_is_refused_before_construction()
    {
        ReaderValidator.Constructions = 0;
        await using var scenario = new CommandScenario<ReaderCommand>().UseDecisionReads();
        var command = new ReaderCommand(EventSourceId.New());
        Assert.Contains("Discoverable validator", (await scenario.Validate(command)).ExceptionMessages.Single());
        Assert.Contains("Discoverable validator", (await scenario.Execute(command)).ExceptionMessages.Single());
        Assert.Equal(0, ReaderValidator.Constructions);
        Assert.Empty(scenario.AppendedEvents);
    }

    public record ConceptName(string Value) : ConceptAs<string>(Value);

    [Command]
    [ProtectedDecision]
    public record NullConceptCommand(EventSourceId EventSourceId, ConceptName? Name)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public class NullConceptValidator : CommandValidator<NullConceptCommand>
    {
        public NullConceptValidator() => RuleFor(_ => _.Name!.Value).NotEmpty();
    }

    [Command]
    [Unprotected]
    public record LegacyOwnershipCommand(EventSourceId EventSourceId)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public class LegacyOwnershipValidator : CommandValidator<LegacyOwnershipCommand>;

    [Command]
    [ProtectedDecision]
    public record OwnershipCommand(EventSourceId EventSourceId)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public class OwnershipValidator : CommandValidator<OwnershipCommand>;

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
        public async Task<when_using_decision_mode.DecisionFinished> Handle(IDecisionReads reads)
        {
            await reads.Get<when_using_decision_mode.DecisionState>((ReadModelKey)EventSourceId);
            Handles++;
            return new(false);
        }
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
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    [Command]
    [ProtectedDecision]
    public record NestedConceptCommand(EventSourceId EventSourceId, NestedConcept Name)
    {
        public static int Handles;
        public when_using_decision_mode.DecisionFinished Handle()
        {
            Handles++;
            return new(false);
        }
    }

    public record NestedConcept(string Value) : ConceptAs<string>(Value);

    public class NestedConceptValidator : AbstractValidator<NestedConcept>, IDiscoverableValidator<NestedConcept>
    {
        public static int Constructions;
        public NestedConceptValidator()
        {
            Constructions++;
            RuleFor(_ => _.Value).Must(_ => throw new InvalidOperationException("Nested rule must not run."));
        }
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
