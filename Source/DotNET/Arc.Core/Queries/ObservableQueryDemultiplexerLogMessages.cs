// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ObservableQueryDemultiplexer"/>.
/// </summary>
internal static partial class ObservableQueryDemultiplexerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "WebSocket client connected to ObservableQueryDemultiplexer")]
    internal static partial void WebSocketClientConnected(this ILogger<ObservableQueryDemultiplexer> logger);

    [LoggerMessage(LogLevel.Debug, "WebSocket client disconnected from ObservableQueryDemultiplexer")]
    internal static partial void WebSocketClientDisconnected(this ILogger<ObservableQueryDemultiplexer> logger);

    [LoggerMessage(LogLevel.Debug, "SSE client connected to ObservableQueryDemultiplexer with connection id '{ConnectionId}'")]
    internal static partial void SseClientConnected(this ILogger<ObservableQueryDemultiplexer> logger, string connectionId);

    [LoggerMessage(LogLevel.Debug, "SSE client disconnected from ObservableQueryDemultiplexer with connection id '{ConnectionId}'")]
    internal static partial void SseClientDisconnected(this ILogger<ObservableQueryDemultiplexer> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "SSE subscribe request for unknown connection id '{ConnectionId}'")]
    internal static partial void SseUnknownConnection(this ILogger<ObservableQueryDemultiplexer> logger, string connectionId);

    [LoggerMessage(LogLevel.Debug, "Client subscribed to query '{QueryName}' with id '{QueryId}'")]
    internal static partial void ClientSubscribed(this ILogger<ObservableQueryDemultiplexer> logger, string queryName, string queryId);

    [LoggerMessage(LogLevel.Debug, "Client unsubscribed from query id '{QueryId}'")]
    internal static partial void ClientUnsubscribed(this ILogger<ObservableQueryDemultiplexer> logger, string queryId);

    [LoggerMessage(LogLevel.Warning, "Client requested subscription with missing query name for id '{QueryId}'")]
    internal static partial void MissingQueryName(this ILogger<ObservableQueryDemultiplexer> logger, string? queryId);

    [LoggerMessage(LogLevel.Warning, "Query '{QueryName}' not found for id '{QueryId}'")]
    internal static partial void QueryNotFound(this ILogger<ObservableQueryDemultiplexer> logger, string queryName, string? queryId);

    [LoggerMessage(LogLevel.Information, "Query '{QueryName}' (id '{QueryId}') is not authorized for the current user")]
    internal static partial void QueryUnauthorized(this ILogger<ObservableQueryDemultiplexer> logger, string queryName, string? queryId);

    [LoggerMessage(LogLevel.Error, "Error sending hub message to client")]
    internal static partial void ErrorSendingMessage(this ILogger<ObservableQueryDemultiplexer> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error processing hub message")]
    internal static partial void ErrorProcessingMessage(this ILogger<ObservableQueryDemultiplexer> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error in observable subscription for query id '{QueryId}'")]
    internal static partial void SubscriptionError(this ILogger<ObservableQueryDemultiplexer> logger, string queryId, Exception ex);
}
