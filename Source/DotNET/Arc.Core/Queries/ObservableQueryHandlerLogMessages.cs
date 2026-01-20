// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ObservableQueryHandler"/>.
/// </summary>
internal static partial class ObservableQueryHandlerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Query {QueryName} returns an observable result")]
    internal static partial void ObservableQueryResult(this ILogger<ObservableQueryHandler> logger, QueryName queryName);

    [LoggerMessage(LogLevel.Debug, "Query {QueryName} returns an async enumerable result")]
    internal static partial void AsyncEnumerableQueryResult(this ILogger<ObservableQueryHandler> logger, QueryName queryName);

    [LoggerMessage(LogLevel.Debug, "Handling streaming result via WebSocket")]
    internal static partial void HandlingAsWebSocket(this ILogger<ObservableQueryHandler> logger);

    [LoggerMessage(LogLevel.Debug, "Handling streaming result via HTTP")]
    internal static partial void HandlingAsHttp(this ILogger<ObservableQueryHandler> logger);

    [LoggerMessage(LogLevel.Error, "Error sending WebSocket message")]
    internal static partial void ErrorSendingWebSocketMessage(this ILogger<ObservableQueryHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Observable error occurred")]
    internal static partial void ObservableError(this ILogger<ObservableQueryHandler> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Streaming observable completed")]
    internal static partial void StreamingObservableCompleted(this ILogger<ObservableQueryHandler> logger);

    [LoggerMessage(LogLevel.Error, "Error enumerating async enumerable")]
    internal static partial void ErrorEnumeratingAsyncEnumerable(this ILogger<ObservableQueryHandler> logger, Exception exception);
}
