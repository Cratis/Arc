// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Extends a <see cref="CommandScenario{TCommand}"/> with an in-memory Chronicle event log.
/// </summary>
/// <remarks>
/// <para>
/// This extender is automatically discovered and invoked by <see cref="CommandScenario{TCommand}"/>
/// when the <c>Cratis.Arc.Chronicle.Testing</c> package is referenced in a test project.
/// No explicit registration is required.
/// </para>
/// <para>
/// After construction the scenario exposes <c>EventLog</c> and <c>Given</c> through
/// C# extension properties defined in <see cref="CommandScenarioChronicleExtensions"/>.
/// </para>
/// </remarks>
public class ChronicleCommandScenarioExtender : ICommandScenarioExtender
{
    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<Type, object> context)
    {
        var eventLog = new EventLogForTesting(Defaults.Instance.EventTypes);
        var given = new EventScenarioGivenBuilder(eventLog);

        services.AddSingleton<Cratis.Chronicle.Events.IEventTypes>(Defaults.Instance.EventTypes);
        services.AddSingleton<IEventLog>(eventLog);
        services.AddSingleton<IEventSequence>(eventLog);

        context[typeof(EventLogForTesting)] = eventLog;
        context[typeof(EventScenarioGivenBuilder)] = given;
    }
}
