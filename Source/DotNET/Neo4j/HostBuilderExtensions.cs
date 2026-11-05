// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Add Neo4j to the solution.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to use Neo4j with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="Neo4jOptions"/>.</param>
    /// <param name="configureNeo4j">The optional callback for configuring <see cref="INeo4jBuilder"/>.</param>
    /// <param name="neo4jConfigSectionPath">Optional string for the <see cref="Neo4jOptions"/> config section path.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    public static IHostBuilder AddCratisNeo4j(
        this IHostBuilder builder,
        Action<Neo4jOptions>? configureOptions = default,
        Action<INeo4jBuilder>? configureNeo4j = default,
        string? neo4jConfigSectionPath = null) => builder.ConfigureServices((context, services) => services.AddCratisNeo4j(
            configureOptions,
            configureNeo4j,
            neo4jConfigSectionPath));
}
