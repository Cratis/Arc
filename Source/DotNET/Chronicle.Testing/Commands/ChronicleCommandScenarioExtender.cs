// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

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

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var eventScenario = new EventScenario();
        var appendedEvents = new List<AppendedEventWithResult>();
        var readModels = new CommandScenarioReadModels(new ReadModelsForTesting(Defaults.Instance.EventStore.ReadModels));
        var eventStore = new EventStoreForScenario(eventScenario, readModels);
        var unitOfWorkManager = new UnitOfWorkManager(eventStore);

        eventScenario.EventLog.AppendOperations.Subscribe(appendedEvents.AddRange);

        services.AddSingleton(Defaults.Instance.EventTypes);
        services.AddSingleton(eventScenario.EventSequence);
        services.AddSingleton<IReadModels>(readModels);
        services.AddReadModels(Defaults.Instance.ClientArtifactsProvider);
        services.AddSingleton<IEventStore>(eventStore);
        services.AddSingleton<IUnitOfWorkManager>(unitOfWorkManager);

        // Make the harness a transactional command scope exactly like production (AddCommandTransactions): appends
        // enroll in the command's unit of work — begun and completed by the discovered TransactionalCommandScope —
        // and commit atomically, or roll back if the command is not successful.
        services.AddSingleton<IEventLog>(new TransactionalEventLog(eventScenario.EventLog, unitOfWorkManager));

        context[ContextKey] = eventScenario;
        context[AppendedEventsKey] = appendedEvents;
        context[ReadModelsKey] = readModels;
    }
}
