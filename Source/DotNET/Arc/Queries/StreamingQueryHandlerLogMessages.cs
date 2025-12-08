// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="StreamingQueryHandler"/>.
/// </summary>
internal static partial class StreamingQueryHandlerLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Query {QueryName} returns an observable result")]
    internal static partial void ObservableQueryResult(this ILogger logger, QueryName queryName);

    [LoggerMessage(1, LogLevel.Debug, "Query {QueryName} returns an async enumerable result")]
    internal static partial void AsyncEnumerableQueryResult(this ILogger logger, QueryName queryName);

    [LoggerMessage(2, LogLevel.Debug, "Handling streaming result via WebSocket")]
    internal static partial void HandlingAsWebSocket(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Handling streaming result via HTTP")]
    internal static partial void HandlingAsHttp(this ILogger logger);

    [LoggerMessage(4, LogLevel.Error, "Error sending WebSocket message")]
    internal static partial void ErrorSendingWebSocketMessage(this ILogger logger, Exception exception);

    [LoggerMessage(5, LogLevel.Error, "Observable error occurred")]
    internal static partial void ObservableError(this ILogger logger, Exception exception);

    [LoggerMessage(6, LogLevel.Debug, "Streaming observable completed")]
    internal static partial void StreamingObservableCompleted(this ILogger logger);

    [LoggerMessage(7, LogLevel.Error, "Error enumerating async enumerable")]
    internal static partial void ErrorEnumeratingAsyncEnumerable(this ILogger logger, Exception exception);
}
