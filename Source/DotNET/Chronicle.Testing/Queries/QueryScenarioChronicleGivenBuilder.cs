// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Testing.Queries;

/// <summary>
/// Selects an event source for query read model seeding.
/// </summary>
/// <typeparam name="TReadModel">The queried read model.</typeparam>
/// <param name="scenario">The query scenario.</param>
public sealed class QueryScenarioChronicleGivenBuilder<TReadModel>(QueryScenario<TReadModel> scenario)
{
    /// <summary>
    /// Selects the event source whose read model state will be seeded.
    /// </summary>
    /// <param name="eventSourceId">The event source id.</param>
    /// <returns>The selected source builder.</returns>
    public QueryScenarioSourceGivenBuilder<TReadModel> ForEventSource(EventSourceId eventSourceId) => new(scenario, eventSourceId);
}
