// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Provides read model state setup for a specific event source id.
/// </summary>
/// <typeparam name="TCommand">Type of command the scenario is for.</typeparam>
public sealed class CommandScenarioSourceGivenBuilder<TCommand>
{
    readonly CommandScenario<TCommand> _scenario;
    readonly EventSourceId _eventSourceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandScenarioSourceGivenBuilder{TCommand}"/> class.
    /// </summary>
    /// <param name="scenario">The command scenario to seed into.</param>
    /// <param name="eventSourceId">The event source id to set up state for.</param>
    internal CommandScenarioSourceGivenBuilder(CommandScenario<TCommand> scenario, EventSourceId eventSourceId)
    {
        _scenario = scenario;
        _eventSourceId = eventSourceId;
    }

    /// <summary>
    /// Seeds the events that happened for the event source. Any read model a command injects for this source is
    /// materialized from these events through its own reducer or projection — no read model type is named here.
    /// </summary>
    /// <param name="events">The events that happened, in order.</param>
    public void Events(params object[] events) =>
        ReadModels().SeedEvents(_eventSourceId, events);

    /// <summary>
    /// Pins a materialized read model instance for the event source, used when a test wants the command to observe a
    /// specific read model value directly instead of deriving it from events.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to pin. Inferred from <paramref name="readModel"/>.</typeparam>
    /// <param name="readModel">The read model instance.</param>
    public void ReadModel<TReadModel>(TReadModel readModel)
        where TReadModel : class =>
        ReadModels().SeedInstance(typeof(TReadModel), _eventSourceId, readModel);

    CommandScenarioReadModels ReadModels() =>
        (CommandScenarioReadModels)_scenario.Context[ChronicleCommandScenarioExtender.ReadModelsKey];
}
