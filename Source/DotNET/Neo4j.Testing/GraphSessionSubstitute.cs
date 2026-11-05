// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Testing;

/// <summary>
/// Represents an <see cref="IGraphSession"/> for specifications that answers queries with the rows you set up, without a Neo4j server.
/// </summary>
/// <remarks>
/// <para>
/// Set up the rows for a query with <see cref="Returns{T}"/>, which matches on a fragment of the Cypher text, then read
/// back what was asked with <see cref="Executions"/>. A query nothing was set up for returns no rows. The parameters are
/// converted exactly as <see cref="IGraphSession"/> does, so a spec sees a concept as the primitive the driver would be given.
/// </para>
/// <para>
/// <see cref="Run"/> and <see cref="Session"/> need a driver and are not supported; use <see cref="Query{T}"/> and
/// <see cref="QuerySingle{T}"/>, which is what application code mapping to its own types uses.
/// </para>
/// </remarks>
public sealed class GraphSessionSubstitute : IGraphSession
{
    readonly List<(string Fragment, Type Type, Func<IReadOnlyList<object?>> Rows)> _setups = [];
    readonly List<GraphExecution> _executions = [];

    /// <inheritdoc/>
    public string DatabaseName { get; set; } = "substitute";

    /// <inheritdoc/>
    public string Namespace { get; set; } = GraphDatabaseName.DefaultNamespace;

    /// <inheritdoc/>
    public GraphDatabaseMode DatabaseMode { get; set; } = GraphDatabaseMode.PerNamespace;

    /// <inheritdoc/>
    public AccessMode AccessMode { get; set; } = AccessMode.Read;

    /// <inheritdoc/>
    public IAsyncSession Session => throw new NotSupportedException("The substitute has no driver session. Use Query or QuerySingle.");

    /// <summary>
    /// Gets the queries that were run, in order.
    /// </summary>
    public IReadOnlyList<GraphExecution> Executions => _executions;

    /// <summary>
    /// Set up the rows a query answers with.
    /// </summary>
    /// <typeparam name="T">The type of the rows.</typeparam>
    /// <param name="cypherFragment">Text the Cypher must contain, ignoring case.</param>
    /// <param name="rows">The rows to answer with.</param>
    /// <returns>The substitute for continuation.</returns>
    public GraphSessionSubstitute Returns<T>(string cypherFragment, params T[] rows)
    {
        _setups.Add((cypherFragment, typeof(T), () => [.. rows.Cast<object?>()]));
        return this;
    }

    /// <inheritdoc/>
    public IDictionary<string, object> Parameters(object? parameters = null)
    {
        var converted = GraphParameterConverter.Convert(parameters);
        converted[GraphParameters.Namespace] = Namespace;
        return converted!;
    }

    /// <inheritdoc/>
    public Task<IResultCursor> Run(string cypher, object? parameters = null) =>
        throw new NotSupportedException("The substitute has no driver. Use Query or QuerySingle.");

    /// <inheritdoc/>
    public Task<IReadOnlyList<T>> Query<T>(string cypher, object? parameters = null)
    {
        _executions.Add(new(cypher, new Dictionary<string, object?>(Parameters(parameters)!)));
        var setup = _setups.LastOrDefault(_ => _.Type == typeof(T) && cypher.Contains(_.Fragment, StringComparison.OrdinalIgnoreCase));
        IReadOnlyList<T> result = setup.Rows is null ? [] : [.. setup.Rows().Cast<T>()];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<T?> QuerySingle<T>(string cypher, object? parameters = null)
    {
        var rows = await Query<T>(cypher, parameters);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
