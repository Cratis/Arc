// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Dispatches writes to outcome-aware or legacy connection handlers.
/// </summary>
internal static class WebSocketSends
{
    /// <summary>
    /// Sends a result without inferring delivery from a legacy null return.
    /// </summary>
    /// <param name="handler">The connection handler.</param>
    /// <param name="webSocket">The connection to write to.</param>
    /// <param name="queryResult">The result to send.</param>
    /// <param name="writeLock">The connection's write lock.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The reported or unconfirmed write outcome.</returns>
    internal static async Task<WebSocketSendResult> Send(IWebSocketConnectionHandler handler, IWebSocket webSocket, QueryResult queryResult, SemaphoreSlim writeLock, CancellationToken token)
    {
        try
        {
            if (handler is IWebSocketMessageSender sender)
            {
                return await sender.Send(webSocket, queryResult, writeLock, token);
            }

            var error = await handler.SendMessage(webSocket, queryResult, writeLock, token);
            return error is null ? WebSocketSendResult.Unconfirmed : WebSocketSendResult.Failed(error);
        }
        catch (Exception error)
        {
            return WebSocketSendResult.Failed(error);
        }
    }
}
