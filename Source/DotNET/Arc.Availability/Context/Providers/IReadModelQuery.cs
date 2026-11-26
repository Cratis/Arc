// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Defines how a read model query is run for a provided value. The host implements it over its own read models.
/// </summary>
public interface IReadModelQuery
{
    /// <summary>
    /// Runs a query that returns one read model instance.
    /// </summary>
    /// <param name="query">The name of the query.</param>
    /// <param name="key">The key the query is run with.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The instance, or <see langword="null"/> when there is no matching row. Throw when the store fails.</returns>
    ValueTask<object?> Execute(string query, object key, CancellationToken cancellationToken);
}
