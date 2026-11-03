// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="WebSocketOriginsStartupWarning"/>.
/// </summary>
internal static partial class WebSocketOriginsStartupWarningLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Cookie authentication is configured and Cratis:Arc:Query:AllowedWebSocketOrigins is not set, so observable query WebSocket connections are accepted from any origin. Set it to the origins that may connect to restrict them")]
    internal static partial void WebSocketOriginsNotRestricted(this ILogger<WebSocketOriginsStartupWarning> logger);
}
