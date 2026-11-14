// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.Testing;
using Cratis.Chronicle;
using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for substituting the graph views in specifications and scenarios.
/// </summary>
public static class GraphServiceCollectionExtensions
{
    /// <summary>
    /// Replace the <see cref="IGraph"/> with a given one, for instance the <c>Graph</c> of a Chronicle <c>GraphScenario</c>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>, for instance a command scenario's <c>Services</c>.</param>
    /// <param name="graph">The <see cref="IGraph"/> to give to everything that asks for one.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection UseGraph(this IServiceCollection services, IGraph graph)
    {
        services.RemoveAll<IGraph>();
        services.AddSingleton(graph);
        return services;
    }

    /// <summary>
    /// Replace the <see cref="IGraph"/> with a new <see cref="GraphSubstitute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="views">The <see cref="IGraphViews"/> the queried views are declared in; discovered from the loaded assemblies when not given.</param>
    /// <returns>The <see cref="GraphSubstitute"/> to set up and inspect.</returns>
    public static GraphSubstitute UseGraphSubstitute(this IServiceCollection services, IGraphViews? views = null)
    {
        var substitute = new GraphSubstitute(views ?? GraphViews.Discover(DefaultClientArtifactsProvider.Default));
        services.UseGraph(substitute);
        return substitute;
    }
}
