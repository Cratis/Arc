// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for configuring Neo4j.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section paths for the Neo4j options.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Neo4j"];

    /// <summary>
    /// Add Neo4j to the solution: the pooled driver, the tenant-aware database naming and the scoped <see cref="IGraphSession"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to use Neo4j with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="Neo4jOptions"/>.</param>
    /// <param name="configureNeo4j">The optional callback for configuring <see cref="INeo4jBuilder"/>.</param>
    /// <param name="neo4jConfigSectionPath">Optional string for the <see cref="Neo4jOptions"/> config section path.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddCratisNeo4j(
        this IServiceCollection services,
        Action<Neo4jOptions>? configureOptions = default,
        Action<INeo4jBuilder>? configureNeo4j = default,
        string? neo4jConfigSectionPath = null)
    {
        var builder = new Neo4jBuilder();
        configureNeo4j?.Invoke(builder);
        builder.Validate();

        var optionsBuilder = services
            .AddOptions<Neo4jOptions>()
            .ValidateOnStart()
            .ValidateDataAnnotations();

        var sectionPath = neo4jConfigSectionPath ?? ConfigurationPath.Combine(DefaultSectionPaths);
        if (!string.IsNullOrWhiteSpace(sectionPath))
        {
            optionsBuilder.BindConfiguration(sectionPath);
        }

        if (configureOptions is not null)
        {
            services.PostConfigure(configureOptions);
        }

        services.AddSingleton(typeof(IGraphNamespaceResolver), builder.NamespaceResolverType);
        services.AddSingleton(typeof(IGraphDatabaseNameResolver), builder.DatabaseNameResolverType);
        services.AddSingleton<INeo4jDriverFactory, Neo4jDriverFactory>();
        services.AddConnectionResolution();
        services.AddSingleton<IGraphConnections, GraphConnections>();

        // Resolved once per scope, so the driver and the database of a scope always come from the same resolution.
        services.AddScoped(sp => sp.GetRequiredService<IGraphConnections>().Resolve());
        services.AddScoped<IGraphSessionFactory, GraphSessionFactory>();
        services.AddScoped(sp => sp.GetRequiredService<IGraphSessionFactory>().Open());
        return services;
    }
}
