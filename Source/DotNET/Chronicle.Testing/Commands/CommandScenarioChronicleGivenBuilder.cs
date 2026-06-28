// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides Chronicle-specific given setup for a <see cref="CommandScenario{TCommand}"/>.
/// </summary>
/// <typeparam name="TCommand">Type of command the scenario is for.</typeparam>
public sealed class CommandScenarioChronicleGivenBuilder<TCommand>
{
    readonly CommandScenario<TCommand> _scenario;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandScenarioChronicleGivenBuilder{TCommand}"/> class.
    /// </summary>
    /// <param name="scenario">The command scenario to set up.</param>
    internal CommandScenarioChronicleGivenBuilder(CommandScenario<TCommand> scenario) =>
        _scenario = scenario;

    /// <summary>
    /// Selects the event source id to set up read model state for.
    /// </summary>
    /// <param name="eventSourceId">The event source id.</param>
    /// <returns>A builder for seeding events or a pinned read model instance for the selected source.</returns>
    public CommandScenarioSourceGivenBuilder<TCommand> ForEventSource(EventSourceId eventSourceId) =>
        new(_scenario, eventSourceId);
}
