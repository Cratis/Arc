// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Graphs;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Discovers the graph views of the application the way the Chronicle client does, and reports the views it left out.
/// </summary>
/// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> the Chronicle client discovers with, or null to use its default.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> the Chronicle client names properties with, or null for camelCase.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
internal sealed class GraphViewsDiscovery(IClientArtifactsProvider? clientArtifacts, INamingPolicy? namingPolicy, ILogger<GraphViewsDiscovery> logger)
{
    /// <summary>
    /// Discover the views.
    /// </summary>
    /// <returns>The <see cref="IGraphViews"/>.</returns>
    public IGraphViews Discover()
    {
        var views = GraphViews.Discover(clientArtifacts ?? DefaultClientArtifactsProvider.Default, namingPolicy);
        foreach (var (type, failure) in views.Failures)
        {
            logger.ViewLeftOut(type.FullName ?? type.Name, failure.Message);
        }

        return views;
    }
}
