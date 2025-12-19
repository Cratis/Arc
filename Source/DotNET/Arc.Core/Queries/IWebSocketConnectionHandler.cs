// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system that knows how to handle the <see cref="WebSocket"/> connection for observable queries.
/// </summary>
public interface IWebSocketConnectionHandler
{
    /// <summary>
    /// Sends a message on the <see cref="WebSocket"/>.
    /// </summary>
    /// <param name="webSocket">The <see cref="WebSocket"/> to listen to.</param>
    /// <param name="queryResult">The <see cref="QueryResult"/> message to write.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/> to use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous action.</returns>
    Task<Exception?> SendMessage(
        WebSocket webSocket,
        QueryResult queryResult,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken token,
        ILogger? logger = null);

    /// <summary>
    /// Handles all incoming web messages on the given <see cref="WebSocket"/>.
    /// </summary>
    /// <param name="webSocket">The <see cref="WebSocket"/> to listen to.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/> to use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous action.</returns>
    Task HandleIncomingMessages(WebSocket webSocket, CancellationToken token, ILogger? logger = default);
}
