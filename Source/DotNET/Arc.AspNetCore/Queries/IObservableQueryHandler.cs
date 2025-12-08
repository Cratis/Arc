// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a handler for WebSocket-based query operations.
/// </summary>
public interface IObservableQueryHandler
{
    /// <summary>
    /// Handles a streaming query result for WebSocket connections in controller actions.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="actionExecutedContext">The <see cref="ActionExecutedContext"/> from the action execution.</param>
    /// <param name="objectResult">The <see cref="ObjectResult"/> containing the streaming data.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleStreamingResult(
        ActionExecutingContext context,
        ActionExecutedContext? actionExecutedContext,
        ObjectResult objectResult);

    /// <summary>
    /// Handles a streaming query result for WebSocket connections in minimal API endpoints.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the request.</param>
    /// <param name="queryName">The name of the query being executed.</param>
    /// <param name="streamingData">The streaming data (Subject or AsyncEnumerable).</param>
    /// <param name="queryContext">The query context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleStreamingResult(
        HttpContext httpContext,
        QueryName queryName,
        object streamingData,
        QueryContext queryContext);

    /// <summary>
    /// Determines if the current request should be handled as a WebSocket connection.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <returns>True if the request should be handled as WebSocket, false otherwise.</returns>
    bool ShouldHandleAsWebSocket(ActionExecutingContext context);

    /// <summary>
    /// Determines if the current request should be handled as a WebSocket connection.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the request should be handled as WebSocket, false otherwise.</returns>
    bool ShouldHandleAsWebSocket(HttpContext httpContext);

    /// <summary>
    /// Determines if the given data is a streaming result that can be handled via WebSocket.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a streaming result, false otherwise.</returns>
    bool IsStreamingResult(object? data);
}