// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a handler for streaming (WebSocket-based) query operations.
/// </summary>
public interface IStreamingQueryHandler
{
    /// <summary>
    /// Determines if the given data is a streaming result that can be handled via WebSocket.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a streaming result, false otherwise.</returns>
    bool IsStreamingResult(object? data);

    /// <summary>
    /// Handles a streaming query result, upgrading to WebSocket if the client requested it.
    /// </summary>
    /// <param name="context">The <see cref="Http.IHttpRequestContext"/>.</param>
    /// <param name="queryName">The name of the query being executed.</param>
    /// <param name="streamingData">The streaming data (Subject or AsyncEnumerable).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleStreamingResult(
        Http.IHttpRequestContext context,
        QueryName queryName,
        object streamingData);
}
