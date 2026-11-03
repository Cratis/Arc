// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for the WebSocket origin check.
/// </summary>
internal static partial class WebSocketOriginLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Refused WebSocket connection to {Path} from origin '{Origin}'. Add the origin to Cratis:Arc:Query:AllowedWebSocketOrigins to accept it")]
    internal static partial void RefusedWebSocketOrigin(this ILogger logger, string path, string origin);
}
