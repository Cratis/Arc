// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
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
/// </para>
/// </remarks>
public class ChronicleCommandScenarioExtender : ICommandScenarioExtender
{
    /// <summary>
    /// The context key used to store the <see cref="EventScenario"/> in the scenario context dictionary.
    /// </summary>
    public const string ContextKey = "Chronicle.EventScenario";

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var eventScenario = new EventScenario();

        services.AddSingleton<Cratis.Chronicle.Events.IEventTypes>(Defaults.Instance.EventTypes);
        services.AddSingleton<IEventLog>(eventScenario.EventLog);
        services.AddSingleton<IEventSequence>(eventScenario.EventSequence);

        context[ContextKey] = eventScenario;
    }
}
