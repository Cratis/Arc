// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
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

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var scenarioId = Guid.NewGuid();
        var eventStoreName = new EventStoreName($"test-event-store-{scenarioId:N}");
        var namespaceName = new EventStoreNamespaceName($"default-{scenarioId:N}");
        var eventScenario = new EventScenario(EventSequenceId.Log, eventStoreName, namespaceName, new DiscoveredConstraintsForScenario(Defaults.Instance));
        var appendedEvents = new List<AppendedEventWithResult>();
        var readModels = new CommandScenarioReadModels(new ReadModelsForTesting(Defaults.Instance.EventStore.ReadModels));
        var eventStore = new EventStoreForScenario(eventScenario, readModels, eventStoreName, namespaceName);
        var unitOfWorkManager = new UnitOfWorkManager(eventStore);

        eventScenario.EventLog.AppendOperations.Subscribe(appendedEvents.AddRange);

        services.AddSingleton(Defaults.Instance.EventTypes);
        services.AddSingleton(eventScenario.EventSequence);
        services.AddSingleton<IReadModels>(readModels);
        services.AddReadModels(Defaults.Instance.ClientArtifactsProvider);
        services.AddSingleton<IEventStore>(eventStore);
        services.AddSingleton<IUnitOfWorkManager>(unitOfWorkManager);

        // The harness's IEventLog is a pure pass-through — appends behave exactly like production: immediate through
        // the in-memory kernel, with the explicit transactional style enrolling in the command's unit of work.
        services.AddSingleton<IEventLog>(new EventLogForScenario(eventScenario.EventLog, unitOfWorkManager));

        context[ContextKey] = eventScenario;
        context[AppendedEventsKey] = appendedEvents;
        context[ReadModelsKey] = readModels;
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
        var store = new EventStoreForTesting(serviceProvider: null, clientArtifactsProvider: Defaults.Instance.ClientArtifactsProvider);
        var commandEvents = new List<AppendedEventWithResult>();
        var scenario = new DecisionCommandScenario(store, commandEvents);
        services.Replace(ServiceDescriptor.Singleton<IEventStore>(store));
        services.Replace(ServiceDescriptor.Singleton<IUnitOfWorkManager>(store.UnitOfWorkManager));

        // Non-decision reads (IReadModels, injected read models and an [Unprotected] command's DecisionRead<T>) resolve a
        // pinned instance or materialize from the same log the protected decision reads fold.
        var readModels = new CommandScenarioReadModels(store.ReadModels, store.EventLog);
        services.Replace(ServiceDescriptor.Singleton<IReadModels>(readModels));
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
        context[DecisionScenarioKey] = scenario;
        context[AppendedEventsKey] = commandEvents;
        context[ReadModelsKey] = readModels;
    }
}
