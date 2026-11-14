// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Cratis.Arc.Neo4j.Chronicle;
using Cratis.Chronicle;
using Cratis.Chronicle.Graphs;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for reading Neo4j through Chronicle graph views.
/// </summary>
public static class GraphViewsServiceCollectionExtensions
{
    /// <summary>
    /// Add graph views over Neo4j: a scoped <see cref="IGraph"/> that model-bound queries, a command's <c>Provide()</c> and
    /// scoped services can take, reading the database of the current tenant through <see cref="IGraphSession"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> Neo4j has been added to with <c>AddCratisNeo4j()</c>.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    /// <remarks>
    /// <para>
    /// Call it once beside <c>AddCratisNeo4j()</c>; it adds nothing else of Neo4j and changes nothing it registered. It
    /// registers <see cref="IGraphViews"/> as a singleton, discovered from the <see cref="IClientArtifactsProvider"/> and
    /// <see cref="INamingPolicy"/> the Chronicle client registered, or from the client's defaults when there are none,
    /// <see cref="IGraphQueryExecutor"/> as the scoped <see cref="Neo4jGraphQueryExecutor"/>, and <see cref="IGraph"/> as a scoped
    /// <see cref="QueryableGraph"/>. Each is added only when it is not registered already, so registering your own first
    /// replaces it.
    /// </para>
    /// <para>
    /// A view that breaks a declaration rule is logged as a warning and left out; querying it throws <see cref="NotAGraphView"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCratisNeo4jGraphViews(this IServiceCollection services)
    {
        services.TryAddSingleton(sp => new GraphViewsDiscovery(
            sp.GetService<IClientArtifactsProvider>(),
            sp.GetService<INamingPolicy>(),
            sp.GetService<ILogger<GraphViewsDiscovery>>() ?? NullLogger<GraphViewsDiscovery>.Instance).Discover());
        services.TryAddScoped<IGraphQueryExecutor>(sp => new Neo4jGraphQueryExecutor(
            sp.GetRequiredService<IGraphSession>(),
            sp.GetService<ILogger<Neo4jGraphQueryExecutor>>() ?? NullLogger<Neo4jGraphQueryExecutor>.Instance));
        services.TryAddScoped<IGraph>(sp => new QueryableGraph(sp.GetRequiredService<IGraphViews>(), sp.GetRequiredService<IGraphQueryExecutor>()));
        return services;
    }
}
