// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphDatabaseNameResolver"/> that names the database from the event
/// store and the namespace of the current tenant using <see cref="GraphDatabaseName"/>.
/// </summary>
/// <remarks>
/// In <see cref="GraphDatabaseMode.Single"/> mode every namespace shares <see cref="Neo4jOptions.SingleDatabase"/>.
/// </remarks>
/// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
/// <param name="namespaceResolver">The <see cref="IGraphNamespaceResolver"/>.</param>
public class DefaultGraphDatabaseNameResolver(IOptionsMonitor<Neo4jOptions> options, IGraphNamespaceResolver namespaceResolver) : IGraphDatabaseNameResolver
{
    /// <inheritdoc/>
    public string Resolve()
    {
        var current = options.CurrentValue;
        return current.DatabaseMode == GraphDatabaseMode.Single
            ? current.SingleDatabase
            : GraphDatabaseName.For(current.DatabasePrefix, current.EventStore, namespaceResolver.Resolve());
    }
}
