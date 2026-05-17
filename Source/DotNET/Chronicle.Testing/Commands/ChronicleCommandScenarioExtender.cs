// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
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

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var eventScenario = new EventScenario();
        var appendedEvents = new List<AppendedEventWithResult>();

        eventScenario.EventLog.AppendOperations.Subscribe(appendedEvents.AddRange);

        services.AddSingleton(Defaults.Instance.EventTypes);
        services.AddSingleton(eventScenario.EventLog);
        services.AddSingleton(eventScenario.EventSequence);
        services.AddSingleton<IEventStore>(_ => new EventStoreForScenario(eventScenario));

        context[ContextKey] = eventScenario;
        context[AppendedEventsKey] = appendedEvents;
    }
}
