// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ControllerObservableQueryAdapter"/>.
/// </summary>
internal static partial class ControllerObservableQueryAdapterLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Controller {Controller} with action {Action} returns a client observable")]
    internal static partial void ClientObservableReturnValue(this ILogger<ControllerObservableQueryAdapter> logger, string controller, string action);

    [LoggerMessage(LogLevel.Trace, "Request is WebSocket")]
    internal static partial void RequestIsWebSocket(this ILogger<ControllerObservableQueryAdapter> logger);

    [LoggerMessage(LogLevel.Trace, "Request is regular HTTP")]
    internal static partial void RequestIsHttp(this ILogger<ControllerObservableQueryAdapter> logger);

    [LoggerMessage(LogLevel.Trace, "Controller {Controller} with action {Action} returns a client enumerable")]
    internal static partial void AsyncEnumerableReturnValue(this ILogger<ControllerObservableQueryAdapter> logger, string controller, string action);
}