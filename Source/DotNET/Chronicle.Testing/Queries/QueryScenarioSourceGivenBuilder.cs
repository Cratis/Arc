// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Testing.Queries;

/// <summary>
/// Seeds Chronicle state for queries that resolve read models from <c>IReadModels</c>.
/// </summary>
/// <typeparam name="TReadModel">The queried read model.</typeparam>
/// <param name="scenario">The query scenario.</param>
/// <param name="eventSourceId">The selected event source id.</param>
public sealed class QueryScenarioSourceGivenBuilder<TReadModel>(QueryScenario<TReadModel> scenario, EventSourceId eventSourceId)
{
    /// <summary>
    /// Pins a materialized read model for the selected event source.
    /// </summary>
    /// <param name="readModel">The read model to pin.</param>
    public void ReadModel(TReadModel readModel) =>
        ((CommandScenarioReadModels)scenario.Context[ChronicleCommandScenarioExtender.ReadModelsKey])
            .SeedInstance(typeof(TReadModel), eventSourceId, readModel!);

    /// <summary>
    /// Pins another read model type for the selected event source.
    /// </summary>
    /// <typeparam name="TOther">The read model type to pin.</typeparam>
    /// <param name="readModel">The read model to pin.</param>
    public void ReadModel<TOther>(TOther readModel)
        where TOther : class =>
        ((CommandScenarioReadModels)scenario.Context[ChronicleCommandScenarioExtender.ReadModelsKey])
            .SeedInstance(typeof(TOther), eventSourceId, readModel);

    /// <summary>
    /// Seeds events to be projected into a read model on demand.
    /// </summary>
    /// <param name="events">The events in occurrence order.</param>
    public void Events(params object[] events) =>
        ((CommandScenarioReadModels)scenario.Context[ChronicleCommandScenarioExtender.ReadModelsKey])
            .SeedEvents(eventSourceId, events);
}
