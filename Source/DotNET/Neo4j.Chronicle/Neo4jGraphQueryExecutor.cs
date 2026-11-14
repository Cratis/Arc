// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IGraphQueryExecutor"/> that runs graph view queries against Neo4j through the
/// tenant-aware <see cref="IGraphSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// The query is translated by <see cref="GraphQueryTranslator"/> for the <see cref="GraphDatabaseMode"/> of the session and
/// run as one read query on the database of the current tenant; the session adds the <c>$namespace</c> parameter. The
/// rows are mapped by <see cref="GraphRowMapper"/>. The Cypher only matches and returns, and the scoped session is a read
/// session, so a graph view never writes.
/// </para>
/// <para>
/// The session gives no way to cancel a query once it runs, so the cancellation token is honored before the query starts and
/// before the rows are mapped.
/// </para>
/// </remarks>
/// <param name="session">The <see cref="IGraphSession"/> on the database of the current tenant.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public class Neo4jGraphQueryExecutor(IGraphSession session, ILogger<Neo4jGraphQueryExecutor> logger) : IGraphQueryExecutor
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<GraphRow>> Execute(GraphQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cypher = GraphQueryTranslator.Translate(query, session.DatabaseMode);
        logger.ExecutingGraphQuery(query.Graph, query.Root.View, session.DatabaseName);
        var rows = await session.Query<object?>(cypher.Text, cypher.Parameters);
        cancellationToken.ThrowIfCancellationRequested();
        return [.. rows.Select(row => GraphRowMapper.Map(row, query.Root))];
    }
}
