// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the optional capability to report actual WebSocket write outcomes.
/// </summary>
public interface IWebSocketMessageSender
{
    /// <summary>
    /// Sends a query result and reports whether the transport write completed.
    /// </summary>
    /// <param name="webSocket">The connection to write to.</param>
    /// <param name="queryResult">The result to send.</param>
    /// <param name="writeLock">The connection's write lock.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The actual write outcome, not an acknowledgment from the client.</returns>
    Task<WebSocketSendResult> Send(IWebSocket webSocket, QueryResult queryResult, SemaphoreSlim writeLock, CancellationToken token);
}
