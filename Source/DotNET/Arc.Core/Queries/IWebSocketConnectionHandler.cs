// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system that knows how to handle the <see cref="IWebSocket"/> connection for observable queries.
/// </summary>
public interface IWebSocketConnectionHandler
{
    /// <summary>
    /// Sends a message on the <see cref="IWebSocket"/>.
    /// </summary>
    /// <param name="webSocket">The <see cref="IWebSocket"/> to send on.</param>
    /// <param name="queryResult">The <see cref="QueryResult"/> message to write.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous action.</returns>
    Task<Exception?> SendMessage(
        IWebSocket webSocket,
        QueryResult queryResult,
        CancellationToken token);

    /// <summary>
    /// Handles all incoming web messages on the given <see cref="IWebSocket"/>.
    /// </summary>
    /// <param name="webSocket">The <see cref="IWebSocket"/> to listen to.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous action.</returns>
    Task HandleIncomingMessages(IWebSocket webSocket, CancellationToken token);
}
