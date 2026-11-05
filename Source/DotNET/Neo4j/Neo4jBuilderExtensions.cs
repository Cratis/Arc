// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Provides extension methods for <see cref="INeo4jBuilder"/>.
/// </summary>
public static class Neo4jBuilderExtensions
{
    /// <summary>
    /// Use a custom <see cref="IGraphDatabaseNameResolver"/>.
    /// </summary>
    /// <typeparam name="T">Type of the resolver.</typeparam>
    /// <param name="builder">The <see cref="INeo4jBuilder"/>.</param>
    /// <returns>The <see cref="INeo4jBuilder"/> for continuation.</returns>
    public static INeo4jBuilder WithDatabaseNameResolver<T>(this INeo4jBuilder builder)
        where T : IGraphDatabaseNameResolver
    {
        builder.DatabaseNameResolverType = typeof(T);
        return builder;
    }

    /// <summary>
    /// Use a custom <see cref="IGraphNamespaceResolver"/>.
    /// </summary>
    /// <typeparam name="T">Type of the resolver.</typeparam>
    /// <param name="builder">The <see cref="INeo4jBuilder"/>.</param>
    /// <returns>The <see cref="INeo4jBuilder"/> for continuation.</returns>
    public static INeo4jBuilder WithNamespaceResolver<T>(this INeo4jBuilder builder)
        where T : IGraphNamespaceResolver
    {
        builder.NamespaceResolverType = typeof(T);
        return builder;
    }
}
