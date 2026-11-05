// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="INeo4jBuilder"/>.
/// </summary>
public class Neo4jBuilder : INeo4jBuilder
{
    /// <inheritdoc/>
    public Type NamespaceResolverType { get; set; } = typeof(TenantGraphNamespaceResolver);

    /// <inheritdoc/>
    public Type DatabaseNameResolverType { get; set; } = typeof(DefaultGraphDatabaseNameResolver);

    /// <inheritdoc/>
    public void Validate()
    {
        Ensure<IGraphNamespaceResolver>(NamespaceResolverType, nameof(NamespaceResolverType));
        Ensure<IGraphDatabaseNameResolver>(DatabaseNameResolverType, nameof(DatabaseNameResolverType));
    }

    static void Ensure<TService>(Type? type, string name)
    {
        if (type is null || !typeof(TService).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
        {
            throw new ArgumentException($"{name} must be a concrete implementation of {typeof(TService).Name}.", name);
        }
    }
}
