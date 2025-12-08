// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a handler for observable (WebSocket-based) query operations.
/// </summary>
public interface IObservableQueryHandler
{
    /// <summary>
    /// Determines if the current request should be handled as a WebSocket connection.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <returns>True if the request should be handled as WebSocket, false otherwise.</returns>
    bool ShouldHandleAsWebSocket(IHttpRequestContext context);

    /// <summary>
    /// Determines if the given data is a streaming result that can be handled via WebSocket.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a streaming result, false otherwise.</returns>
    bool IsStreamingResult(object? data);

    /// <summary>
    /// Handles a streaming query result for WebSocket connections.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <param name="queryName">The name of the query being executed.</param>
    /// <param name="streamingData">The streaming data (Subject or AsyncEnumerable).</param>
    /// <param name="queryContext">The query context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleStreamingResult(
        IHttpRequestContext context,
        QueryName queryName,
        object streamingData,
        QueryContext queryContext);
}
