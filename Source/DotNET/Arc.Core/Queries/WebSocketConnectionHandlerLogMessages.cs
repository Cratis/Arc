// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

/// <summary>
/// Log messages for <see cref="WebSocketConnectionHandler"/>.
/// </summary>
internal static partial class WebSocketConnectionHandlerLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Received message")]
    internal static partial void ReceivedMessage(this ILogger<WebSocketConnectionHandler> logger);

    [LoggerMessage(LogLevel.Trace, "Received ping message")]
    internal static partial void ReceivedPingMessage(this ILogger<WebSocketConnectionHandler> logger);

    [LoggerMessage(LogLevel.Trace, "Sent pong message")]
    internal static partial void SentPongMessage(this ILogger<WebSocketConnectionHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Error handling ping message")]
    internal static partial void ErrorHandlingPingMessage(this ILogger<WebSocketConnectionHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Closing connection. Description: {Description}")]
    internal static partial void CloseConnection(this ILogger<WebSocketConnectionHandler> logger, string? description);

    [LoggerMessage(LogLevel.Debug, "WebSocket error while receiving messages")]
    internal static partial void WebSocketErrorReceivingMessage(this ILogger<WebSocketConnectionHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Operation was cancelled")]
    internal static partial void OperationCancelled(this ILogger<WebSocketConnectionHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Error while receiving messages")]
    internal static partial void ErrorReceivingMessage(this ILogger<WebSocketConnectionHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Client disconnected")]
    internal static partial void ClientDisconnected(this ILogger<WebSocketConnectionHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Error sending message")]
    internal static partial void ErrorSendingMessage(this ILogger<WebSocketConnectionHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "WebSocket is not open (state: {State}), skipping send")]
    internal static partial void WebSocketNotOpen(this ILogger<WebSocketConnectionHandler> logger, System.Net.WebSockets.WebSocketState state);
}
