// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for <see cref="IArcBuilder"/> for reading Neo4j through Chronicle graph views.
/// </summary>
public static class GraphViewsArcBuilderExtensions
{
    /// <summary>
    /// Add graph views over Neo4j, so model-bound queries and a command's <c>Provide()</c> can take a scoped <see cref="IGraph"/>
    /// that reads the database of the current tenant. Use it after <c>WithNeo4j()</c>.
    /// </summary>
    /// <param name="arcBuilder"><see cref="IArcBuilder"/> Neo4j has been added to.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    public static IArcBuilder WithNeo4jGraphViews(this IArcBuilder arcBuilder)
    {
        arcBuilder.Services.AddCratisNeo4jGraphViews();
        return arcBuilder;
    }
}
