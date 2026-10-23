// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Extends a <see cref="CommandScenario{TCommand}"/> with an in-memory Chronicle event scenario.
/// </summary>
/// <remarks>
/// <para>
/// This extender is automatically discovered and invoked by <see cref="CommandScenario{TCommand}"/>
/// when the <c>Cratis.Arc.Chronicle.Testing</c> package is referenced in a test project.
/// No explicit registration is required.
/// </para>
/// <para>
/// After construction the scenario exposes an <see cref="EventScenario"/> through
/// the C# extension property defined in <see cref="CommandScenarioChronicleExtensions"/>.
/// A command that declares read protection or protected dependencies gets only the decision store instead;
/// no <see cref="EventScenario"/> is built and its context key is absent.
/// Events appended during command execution are also captured via the <c>AppendOperations</c>
/// observable and exposed through the <c>AppendedEvents</c> extension property defined in
/// <see cref="CommandScenarioChronicleExtensions"/>.
/// </para>
/// </remarks>
public class ChronicleCommandScenarioExtender : ICommandScenarioExtender
{
    /// <summary>
    /// The context key used to store the <see cref="EventScenario"/> in the scenario context dictionary.
    /// </summary>
    public const string ContextKey = "Chronicle.EventScenario";

    /// <summary>
    /// The context key used to store the list of events appended during command execution.
    /// </summary>
    public const string AppendedEventsKey = "Chronicle.AppendedEvents";

    /// <summary>
    /// The context key used to store the <see cref="CommandScenarioReadModels"/> that seeded read model state is held in.
    /// </summary>
    internal const string ReadModelsKey = "Chronicle.ReadModels";

    /// <summary>
    /// Key for the opt-in single-log decision scenario.
    /// </summary>
    internal const string DecisionScenarioKey = "Chronicle.DecisionScenario";

    /// <summary>
    /// Key for competing appends in a scenario that keeps its ordinary read mode.
    /// </summary>
    internal const string ConcurrentScenarioKey = "Chronicle.ConcurrentScenario";

    /// <summary>
    /// The context key for the serializer registration owned by this extender.
    /// </summary>
    internal const string EventSerializerRegistrationKey = "Chronicle.EventSerializerRegistration";

    const string EventStoreNameKey = "Chronicle.EventStoreName";
    const string EventStoreNamespaceKey = "Chronicle.EventStoreNamespace";

    /// <inheritdoc/>
    public void Extend(Type commandType, IServiceCollection services, IDictionary<string, object> context)
    {
        if (NeedsDecisionReads(commandType))
        {
            ExtendForDecisionReads(services, context);
            return;
        }

        Extend(services, context);
    }

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var (eventStoreName, namespaceName) = NewScenarioIdentity();
        var eventScenario = new EventScenario(EventSequenceId.Log, eventStoreName, namespaceName, new DiscoveredConstraintsForScenario(Defaults.Instance));
        var appendedEvents = new List<AppendedEventWithResult>();
        var readModels = new CommandScenarioReadModels(
            Defaults.Instance.EventStore.ReadModels,
            commandLog: eventScenario.EventLog);
        var eventStore = new EventStoreForScenario(eventScenario, readModels, eventStoreName, namespaceName);
        var unitOfWorkManager = new UnitOfWorkManager(eventStore);

        eventScenario.EventLog.AppendOperations.Subscribe(appendedEvents.AddRange);

        var eventSerializerRegistration = ServiceDescriptor.Singleton(Defaults.Instance.EventSerializer);
        services.TryAdd(eventSerializerRegistration);

        services.AddSingleton(Defaults.Instance.EventTypes);
        services.AddSingleton(eventScenario.EventSequence);
        services.AddSingleton<IReadModels>(readModels);
        services.AddReadModels(Defaults.Instance.ClientArtifactsProvider);
        services.AddSingleton<IEventStore>(eventStore);

        // Resolve from the current store so opting into decision reads uses that store's discovered definitions.
        services.TryAddSingleton<IEventSources>(provider => provider.GetRequiredService<IEventStore>().EventSources);
        services.AddSingleton<IUnitOfWorkManager>(unitOfWorkManager);

        // The harness's IEventLog is a pure pass-through — appends behave exactly like production: immediate through
        // the in-memory kernel, with the explicit transactional style enrolling in the command's unit of work.
        services.AddSingleton<IEventLog>(new EventLogForScenario(eventScenario.EventLog, unitOfWorkManager));

        context[EventStoreNameKey] = eventStoreName;
        context[EventStoreNamespaceKey] = namespaceName;
        context[EventSerializerRegistrationKey] = eventSerializerRegistration;
        context[ContextKey] = eventScenario;
        context[AppendedEventsKey] = appendedEvents;
        context[ReadModelsKey] = readModels;
        context[nameof(GeneratedIdentifierCollisionScenarioCheck)] = new GeneratedIdentifierCollisionScenarioCheck(context, readModels);
    }

    /// <summary>
    /// Enables competing appends on the existing scenario log without opting into decision reads.
    /// </summary>
    /// <param name="services">The services of the uninitialized scenario.</param>
    /// <param name="context">The scenario context.</param>
    /// <returns>The competing append queue for the existing log.</returns>
    /// <exception cref="ConcurrentAppendsCannotOrderCustomExecutionScopes">Custom execution scopes cannot be safely ordered around the owner.</exception>
    internal static DecisionCommandScenario EnableConcurrentAppends(IServiceCollection services, IDictionary<string, object> context)
    {
        if (services.Any(_ => _.ServiceType == typeof(IInstancesOf<ICommandExecutionScope>) || _.ServiceType == typeof(ICommandExecutionScope)))
        {
            throw new ConcurrentAppendsCannotOrderCustomExecutionScopes();
        }

        var eventScenario = (EventScenario)context[ContextKey];
        var unitOfWorkManager = (IUnitOfWorkManager)services.Last(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ImplementationInstance!;
        var scenario = new DecisionCommandScenario(eventScenario.EventLog, unitOfWorkManager, (List<AppendedEventWithResult>)context[AppendedEventsKey], captureCommands: false);
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(sp =>
        {
            var types = TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple<ICommandExecutionScope>()
                .Where(type => type != typeof(DecisionScenarioConcurrentAppendScope) && type != typeof(DecisionScenarioCommandCaptureScope));
            var scopes = types.Select(type => (ICommandExecutionScope)ActivatorUtilities.GetServiceOrCreateInstance(sp, type)).ToList();
            var ownerIndex = scopes.FindIndex(scope => scope is TransactionalCommandScope);
            if (ownerIndex < 0)
            {
                throw new ConcurrentAppendsRequireTransactionalCommandScope();
            }

            scopes.Insert(ownerIndex, new DecisionScenarioCommandCaptureScope(scenario));
            scopes.Insert(ownerIndex + 2, new DecisionScenarioConcurrentAppendScope(scenario));
            return new KnownInstancesOf<ICommandExecutionScope>(scopes);
        });
        context[ConcurrentScenarioKey] = scenario;

        return scenario;
    }

    /// <summary>
    /// Switches a scenario to Chronicle's in-process protected decision reader before its first execution.
    /// </summary>
    /// <param name="services">The services of the uninitialized scenario.</param>
    /// <param name="context">The scenario context.</param>
    /// <exception cref="DecisionReadsMustBeEnabledBeforeSeeding">Legacy state has already been seeded.</exception>
    /// <exception cref="DecisionScenarioCannotOrderCustomExecutionScopes">Custom execution scopes cannot be safely ordered around the owner.</exception>
    /// <exception cref="DecisionScenarioRequiresTransactionalCommandScope">No transactional scope was discovered.</exception>
    internal static void EnableDecisionReads(IServiceCollection services, IDictionary<string, object> context)
    {
        if (context.ContainsKey(DecisionScenarioKey))
        {
            return;
        }

        if (((CommandScenarioReadModels)context[ReadModelsKey]).HasSeededState() ||
            !((EventScenario)context[ContextKey]).EventLog.GetTailSequenceNumber().GetAwaiter().GetResult().IsUnavailable)
        {
            throw new DecisionReadsMustBeEnabledBeforeSeeding();
        }
        if (services.Any(_ => _.ServiceType == typeof(IInstancesOf<ICommandExecutionScope>) || _.ServiceType == typeof(ICommandExecutionScope)))
        {
            throw new DecisionScenarioCannotOrderCustomExecutionScopes();
        }

        var decision = CreateDecisionStore((EventStoreName)context[EventStoreNameKey], (EventStoreNamespaceName)context[EventStoreNamespaceKey]);

        // The IEventSources factory follows the replaced store. IEventStore does not expose a serializer,
        // so replace only our own registration in place, retaining the precedence of explicit overrides.
        var serializerRegistration = (ServiceDescriptor)context[EventSerializerRegistrationKey];
        var serializerIndex = services.IndexOf(serializerRegistration);
        if (serializerIndex >= 0)
        {
            services[serializerIndex] = ServiceDescriptor.Singleton<IEventSerializer>(decision.Defaults.EventSerializer);
        }

        RegisterDecisionStore(services, decision);
        context[DecisionScenarioKey] = decision.Scenario;
        context[AppendedEventsKey] = decision.CommandEvents;
        context[ReadModelsKey] = decision.ReadModels;
    }

    /// <summary>
    /// Builds a decision scenario directly, without the legacy <see cref="EventScenario"/> that is discarded
    /// when a command declares read protection or protected dependencies.
    /// </summary>
    /// <param name="services">The services of the uninitialized scenario.</param>
    /// <param name="context">The scenario context.</param>
    /// <exception cref="DecisionScenarioCannotOrderCustomExecutionScopes">Custom execution scopes cannot be safely ordered around the owner.</exception>
    static void ExtendForDecisionReads(IServiceCollection services, IDictionary<string, object> context)
    {
        var (eventStoreName, namespaceName) = NewScenarioIdentity();
        if (services.Any(_ => _.ServiceType == typeof(IInstancesOf<ICommandExecutionScope>) || _.ServiceType == typeof(ICommandExecutionScope)))
        {
            throw new DecisionScenarioCannotOrderCustomExecutionScopes();
        }

        var decision = CreateDecisionStore(eventStoreName, namespaceName);

        // The registrations follow the order the explicit opt-in leaves behind, so both routes resolve identically.
        var eventSerializerRegistration = ServiceDescriptor.Singleton<IEventSerializer>(decision.Defaults.EventSerializer);
        services.TryAdd(eventSerializerRegistration);
        services.AddSingleton(Defaults.Instance.EventTypes);
        services.AddReadModels(Defaults.Instance.ClientArtifactsProvider);
        services.TryAddSingleton<IEventSources>(provider => provider.GetRequiredService<IEventStore>().EventSources);
        RegisterDecisionStore(services, decision);

        context[EventStoreNameKey] = eventStoreName;
        context[EventStoreNamespaceKey] = namespaceName;
        context[EventSerializerRegistrationKey] = eventSerializerRegistration;
        context[AppendedEventsKey] = decision.CommandEvents;
        context[ReadModelsKey] = decision.ReadModels;
        context[nameof(GeneratedIdentifierCollisionScenarioCheck)] = new GeneratedIdentifierCollisionScenarioCheck(context, decision.ReadModels);
        context[DecisionScenarioKey] = decision.Scenario;
    }

    static (EventStoreName Name, EventStoreNamespaceName Namespace) NewScenarioIdentity()
    {
        var scenarioId = Guid.NewGuid();
        return (new EventStoreName($"test-event-store-{scenarioId:N}"), new EventStoreNamespaceName($"default-{scenarioId:N}"));
    }

    static DecisionStore CreateDecisionStore(EventStoreName eventStoreName, EventStoreNamespaceName namespaceName)
    {
        // Defaults exposes the serializer paired with its testing store; the store's serializer is internal.
        var decisionDefaults = new Defaults(
            Defaults.Instance.ClientArtifactsProvider,
            serviceProvider: null,
            options: null,
            time: null,
            eventStore: eventStoreName,
            @namespace: namespaceName);
        var store = (EventStoreForTesting)decisionDefaults.EventStore;
        var commandEvents = new List<AppendedEventWithResult>();

        // Non-decision reads (IReadModels, injected read models and an [Unprotected] command's DecisionRead<T>) resolve a
        // pinned instance or materialize from the same log the protected decision reads fold.
#pragma warning disable CA2000 // The read models are kept in the scenario context, which disposes them with the scenario.
        return new(
            decisionDefaults,
            store,
            new DecisionCommandScenario(store, commandEvents),
            commandEvents,
            new CommandScenarioReadModels(store.ReadModels, store.EventLog));
#pragma warning restore CA2000
    }

    static void RegisterDecisionStore(IServiceCollection services, DecisionStore decision)
    {
        var store = decision.Store;
        var scenario = decision.Scenario;
        services.Replace(ServiceDescriptor.Singleton<IEventStore>(store));
        services.Replace(ServiceDescriptor.Singleton<IUnitOfWorkManager>(store.UnitOfWorkManager));
        services.Replace(ServiceDescriptor.Singleton<IReadModels>(decision.ReadModels));
        services.Replace(ServiceDescriptor.Singleton<IEventLog>(scenario.EventLog));
        services.Replace(ServiceDescriptor.Singleton<IEventSequence>(store.EventLog));
        services.AddCommandAwareDecisionReads();

        // Explicit ordering is essential: scopes complete in reverse order. The competitor must append before the
        // transactional scope completes, otherwise the assertion would be a false positive after owner commit.
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(sp =>
        {
            var types = TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple<ICommandExecutionScope>()
                .Where(type => type != typeof(DecisionScenarioConcurrentAppendScope) && type != typeof(DecisionScenarioCommandCaptureScope))
                .ToArray();
            var scopes = types.Select(type => (ICommandExecutionScope)ActivatorUtilities.GetServiceOrCreateInstance(sp, type)).ToList();
            var ownerIndex = scopes.FindIndex(scope => scope is TransactionalCommandScope);
            if (ownerIndex < 0)
            {
                throw new DecisionScenarioRequiresTransactionalCommandScope();
            }

            scopes.Insert(ownerIndex, new DecisionScenarioCommandCaptureScope(scenario));
            scopes.Insert(ownerIndex + 2, new DecisionScenarioConcurrentAppendScope(scenario));
            return new KnownInstancesOf<ICommandExecutionScope>(scopes);
        });
    }

    static bool NeedsDecisionReads(Type commandType)
    {
        // Provision before the test registers its host configuration. The real pipeline still applies
        // the operational cap on every execution; provisioning a store never enables a guard by itself.
        using var profile = CommandDecisionPolicy.Begin(commandType);
        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Protected || CommandDecisionPolicy.ReadProtection != ReadProtectionMode.Off)
        {
            return true;
        }

        var dependencies = commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(_ => _.Name == "Handle" || _.Name == "Provide")
            .SelectMany(_ => _.GetParameters());
        var validators = TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple(typeof(IDiscoverableValidator<>))
            .Where(_ => typeof(IDiscoverableValidator<>).MakeGenericType(commandType).IsAssignableFrom(_));
        return dependencies.Concat(validators.SelectMany(_ => _.GetConstructors()).SelectMany(_ => _.GetParameters()))
            .Any(_ => IsDecisionDependency(_.ParameterType) ||
                _.GetCustomAttribute<GeneratedValueAttribute>()?.Freshness == GeneratedValueFreshness.Guarded);
    }

    static bool IsDecisionDependency(Type type) =>
        type == typeof(IDecisionReads) || type == typeof(IStreamReads) || type == typeof(StreamDecision) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>)) ||
        PerStreamReadModelBinding.IsDeclared(type);

    sealed record DecisionStore(
        Defaults Defaults,
        EventStoreForTesting Store,
        DecisionCommandScenario Scenario,
        List<AppendedEventWithResult> CommandEvents,
        CommandScenarioReadModels ReadModels);
}
