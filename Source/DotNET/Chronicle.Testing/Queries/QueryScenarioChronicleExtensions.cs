// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Chronicle.Testing.Queries;

/// <summary>
/// Adds Chronicle read model state setup to query scenarios.
/// </summary>
public static class QueryScenarioChronicleExtensions
{
    extension<TReadModel>(QueryScenario<TReadModel> scenario)
    {
        /// <summary>
        /// Gets a builder for pinning a read model at an event source id.
        /// </summary>
        public QueryScenarioChronicleGivenBuilder<TReadModel> Given => new(scenario);
    }
}
