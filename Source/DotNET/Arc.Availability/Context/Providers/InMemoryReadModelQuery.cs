// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// An in-memory <see cref="IReadModelQuery"/> over rows registered per query and key. Counts executions.
/// </summary>
[IgnoreConvention]
public class InMemoryReadModelQuery : IReadModelQuery
{
    readonly ConcurrentDictionary<(string Query, object Key), object> _rows = new();
    int _executions;

    /// <summary>
    /// Gets how many times a query has been executed.
    /// </summary>
    public int Executions => _executions;

    /// <summary>
    /// Registers the row a query returns for a key.
    /// </summary>
    /// <param name="query">The name of the query.</param>
    /// <param name="key">The key.</param>
    /// <param name="row">The row.</param>
    public void Add(string query, object key, object row) => _rows[(query, key)] = row;

    /// <inheritdoc/>
    public ValueTask<object?> Execute(string query, object key, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _executions);
        return ValueTask.FromResult(_rows.TryGetValue((query, key), out var row) ? row : null);
    }
}
