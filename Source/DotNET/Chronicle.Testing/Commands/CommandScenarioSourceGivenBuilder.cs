// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Streams;
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
    EventRoute? _route;

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
    /// Selects the route used to seed prior events.
    /// </summary>
    /// <param name="route">The route to seed.</param>
    /// <returns>The builder for seeding events on that route.</returns>
    public CommandScenarioSourceGivenBuilder<TCommand> OnRoute(EventRoute route)
    {
        _route = route;

        return this;
    }

    /// <summary>
    /// Seeds the events that happened for the event source. Any read model a command injects for this source is
    /// materialized from these events through its own reducer or projection — no read model type is named here.
    /// </summary>
    /// <param name="events">The events that happened, in order.</param>
    /// <exception cref="RoutedEventSeedingRequiresDecisionReads">Routed seeding was requested without decision mode.</exception>
    public void Events(params object[] events)
    {
        if (_scenario.Context.TryGetValue(ChronicleCommandScenarioExtender.DecisionScenarioKey, out var decision))
        {
            ((DecisionCommandScenario)decision).Seed(_eventSourceId, events, _route).GetAwaiter().GetResult();
            return;
        }
        if (_route is not null)
        {
            throw new RoutedEventSeedingRequiresDecisionReads();
        }
        ReadModels().SeedEvents(_eventSourceId, events);
    }

    /// <summary>
    /// Pins a materialized read model instance for the event source, used when a test wants the command to observe a
    /// specific read model value directly instead of deriving it from events.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to pin. Inferred from <paramref name="readModel"/>.</typeparam>
    /// <param name="readModel">The read model instance.</param>
    /// <remarks>
    /// A plain model under <c>[ReadProtection(On)]</c> uses the pinned state after acquiring its real protected read;
    /// admission, enrollment and conflict detection still run against the event log. A pin does not replace a
    /// <c>DecisionRead&lt;T&gt;</c> token. Those reads, and per-stream models, must be seeded with routed events.
    /// </remarks>
    /// <exception cref="PinnedReadModelCannotProvideDecisionToken">The command reads it as a protected decision read parameter.</exception>
    /// <exception cref="PinnedPerStreamReadModelIsNotSupported">The model is per-stream.</exception>
    public void ReadModel<TReadModel>(TReadModel readModel)
        where TReadModel : class
    {
        if (PerStreamReadModelBinding.IsDeclared(typeof(TReadModel)))
        {
            throw new PinnedPerStreamReadModelIsNotSupported(typeof(TReadModel));
        }
        if (_scenario.Context.ContainsKey(ChronicleCommandScenarioExtender.DecisionScenarioKey) &&
            ProtectedDecisionReadParameters.ReadModelTypesOf(typeof(TCommand)).Contains(typeof(TReadModel)))
        {
            throw new PinnedReadModelCannotProvideDecisionToken(typeof(TCommand), typeof(TReadModel));
        }

        ReadModels().SeedInstance(typeof(TReadModel), _eventSourceId, readModel);
    }

    CommandScenarioReadModels ReadModels() =>
        (CommandScenarioReadModels)_scenario.Context[ChronicleCommandScenarioExtender.ReadModelsKey];
}
