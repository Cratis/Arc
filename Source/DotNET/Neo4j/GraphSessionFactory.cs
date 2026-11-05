// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSessionFactory"/> that opens sessions on a scope's resolved connection.
/// </summary>
/// <param name="connection">The <see cref="GraphConnection"/> resolved once for the scope.</param>
/// <param name="namespaceResolver">The <see cref="IGraphNamespaceResolver"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="Neo4jOptions"/>.</param>
public class GraphSessionFactory(GraphConnection connection, IGraphNamespaceResolver namespaceResolver, IOptions<Neo4jOptions> options) : IGraphSessionFactory
{
    /// <inheritdoc/>
    public IGraphSession Open(AccessMode accessMode = AccessMode.Read) =>
        new GraphSession(connection, namespaceResolver.Resolve(), options.Value.DatabaseMode, accessMode);
}
