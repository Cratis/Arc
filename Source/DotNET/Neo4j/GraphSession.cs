// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSession"/>.
/// </summary>
/// <param name="connection">The <see cref="GraphConnection"/> of the current tenant.</param>
/// <param name="namespace">The name of the namespace of the current tenant.</param>
/// <param name="databaseMode">How the graphs of the namespaces are placed.</param>
/// <param name="accessMode">The <see cref="AccessMode"/> of the session.</param>
public sealed class GraphSession(GraphConnection connection, string @namespace, GraphDatabaseMode databaseMode, AccessMode accessMode) : IGraphSession
{
    readonly Lazy<IAsyncSession> _session = new(() => connection.Driver.AsyncSession(_ => _
        .WithDatabase(connection.DatabaseName)
        .WithDefaultAccessMode(accessMode)));

    /// <inheritdoc/>
    public string DatabaseName => connection.DatabaseName;

    /// <inheritdoc/>
    public string Namespace => @namespace;

    /// <inheritdoc/>
    public GraphDatabaseMode DatabaseMode => databaseMode;

    /// <inheritdoc/>
    public AccessMode AccessMode => accessMode;

    /// <inheritdoc/>
    public IAsyncSession Session => _session.Value;

    /// <inheritdoc/>
    public IDictionary<string, object> Parameters(object? parameters = null)
    {
        var converted = GraphParameterConverter.Convert(parameters);
        converted[GraphParameters.Namespace] = @namespace;
        return converted!;
    }

    /// <inheritdoc/>
    public Task<IResultCursor> Run(string cypher, object? parameters = null) => Session.RunAsync(cypher, Parameters(parameters));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<T>> Query<T>(string cypher, object? parameters = null)
    {
        var cursor = await Run(cypher, parameters);
        var records = await cursor.ToListAsync();
        return [.. records.Select(GraphRecordMapper.Map<T>)];
    }

    /// <inheritdoc/>
    public async Task<T?> QuerySingle<T>(string cypher, object? parameters = null)
    {
        var cursor = await Run(cypher, parameters);
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? default : GraphRecordMapper.Map<T>(records[0]);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_session.IsValueCreated)
        {
            await _session.Value.DisposeAsync();
        }
    }
}
