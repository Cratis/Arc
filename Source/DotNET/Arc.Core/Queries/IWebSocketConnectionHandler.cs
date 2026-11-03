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
    /// <param name="writeLock">A per-connection <see cref="SemaphoreSlim"/> that serializes all sends on this socket.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>The send error, or null when no error was reported. Null does not confirm that a transport write completed.</returns>
    /// <remarks>
    /// Implement <see cref="IWebSocketMessageSender"/> to report explicit write outcomes. The built-in handler also returns
    /// null for skipped or unconfirmed writes; consumers must not interpret null as delivery confirmation.
    /// </remarks>
    Task<Exception?> SendMessage(
        IWebSocket webSocket,
        QueryResult queryResult,
        SemaphoreSlim writeLock,
        CancellationToken token);

    /// <summary>
    /// Handles all incoming web messages on the given <see cref="IWebSocket"/>.
    /// </summary>
    /// <param name="webSocket">The <see cref="IWebSocket"/> to listen to.</param>
    /// <param name="writeLock">A per-connection <see cref="SemaphoreSlim"/> that serializes all sends on this socket.</param>
    /// <param name="token">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous action.</returns>
    Task HandleIncomingMessages(IWebSocket webSocket, SemaphoreSlim writeLock, CancellationToken token);
}
