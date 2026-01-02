// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Represents the result of performing an observable query through the JavaScript proxy.
/// </summary>
/// <typeparam name="TResult">The type of the data.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ObservableQueryExecutionResult{TResult}"/> class.
/// </remarks>
/// <param name="result">The initial query result.</param>
/// <param name="updates">All updates received.</param>
public class ObservableQueryExecutionResult<TResult>(Queries.QueryResult result, List<TResult> updates)
{
    /// <summary>
    /// Gets the initial query result.
    /// </summary>
    public Queries.QueryResult Result { get; } = result;

    /// <summary>
    /// Gets all updates received.
    /// </summary>
    public List<TResult> Updates { get; } = updates;

    /// <summary>
    /// Gets the most recent data from updates.
    /// </summary>
    public TResult LatestData => Updates[^1];
}
