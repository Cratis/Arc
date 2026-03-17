// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ObservableQueryHub"/>.
/// </summary>
internal static partial class ObservableQueryHubLogMessages
{
    [LoggerMessage(LogLevel.Debug, "WebSocket client connected to ObservableQueryHub")]
    internal static partial void WebSocketClientConnected(this ILogger<ObservableQueryHub> logger);

    [LoggerMessage(LogLevel.Debug, "WebSocket client disconnected from ObservableQueryHub")]
    internal static partial void WebSocketClientDisconnected(this ILogger<ObservableQueryHub> logger);

    [LoggerMessage(LogLevel.Debug, "SSE client connected to ObservableQueryHub for query '{QueryName}'")]
    internal static partial void SseClientConnected(this ILogger<ObservableQueryHub> logger, string queryName);

    [LoggerMessage(LogLevel.Debug, "SSE client disconnected from ObservableQueryHub")]
    internal static partial void SseClientDisconnected(this ILogger<ObservableQueryHub> logger);

    [LoggerMessage(LogLevel.Debug, "Client subscribed to query '{QueryName}' with id '{QueryId}'")]
    internal static partial void ClientSubscribed(this ILogger<ObservableQueryHub> logger, string queryName, string queryId);

    [LoggerMessage(LogLevel.Debug, "Client unsubscribed from query id '{QueryId}'")]
    internal static partial void ClientUnsubscribed(this ILogger<ObservableQueryHub> logger, string queryId);

    [LoggerMessage(LogLevel.Warning, "Client requested subscription with missing query name for id '{QueryId}'")]
    internal static partial void MissingQueryName(this ILogger<ObservableQueryHub> logger, string? queryId);

    [LoggerMessage(LogLevel.Warning, "Query '{QueryName}' not found for id '{QueryId}'")]
    internal static partial void QueryNotFound(this ILogger<ObservableQueryHub> logger, string queryName, string? queryId);

    [LoggerMessage(LogLevel.Information, "Query '{QueryName}' (id '{QueryId}') is not authorized for the current user")]
    internal static partial void QueryUnauthorized(this ILogger<ObservableQueryHub> logger, string queryName, string? queryId);

    [LoggerMessage(LogLevel.Error, "Error sending hub message to client")]
    internal static partial void ErrorSendingMessage(this ILogger<ObservableQueryHub> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error processing hub message")]
    internal static partial void ErrorProcessingMessage(this ILogger<ObservableQueryHub> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error in observable subscription for query id '{QueryId}'")]
    internal static partial void SubscriptionError(this ILogger<ObservableQueryHub> logger, string queryId, Exception ex);

    [LoggerMessage(LogLevel.Debug, "SSE query '{QueryName}' missing required 'query' parameter")]
    internal static partial void SseMissingQueryParameter(this ILogger<ObservableQueryHub> logger, string queryName);
}
