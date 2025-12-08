// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ObservableQueryHandler"/>.
/// </summary>
internal static partial class ObservableQueryHandlerLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Controller {Controller} with action {Action} returns a client observable")]
    internal static partial void ClientObservableReturnValue(this ILogger logger, string controller, string action);

    [LoggerMessage(1, LogLevel.Trace, "Request is WebSocket")]
    internal static partial void RequestIsWebSocket(this ILogger logger);

    [LoggerMessage(2, LogLevel.Trace, "Request is regular HTTP")]
    internal static partial void RequestIsHttp(this ILogger logger);

    [LoggerMessage(3, LogLevel.Trace, "Controller {Controller} with action {Action} returns a client enumerable")]
    internal static partial void AsyncEnumerableReturnValue(this ILogger logger, string controller, string action);

    [LoggerMessage(4, LogLevel.Trace, "Endpoint query {QueryName} returns a client observable")]
    internal static partial void EndpointObservableReturnValue(this ILogger logger, QueryName queryName);

    [LoggerMessage(5, LogLevel.Trace, "Endpoint query {QueryName} returns a client enumerable")]
    internal static partial void EndpointEnumerableReturnValue(this ILogger logger, QueryName queryName);
}