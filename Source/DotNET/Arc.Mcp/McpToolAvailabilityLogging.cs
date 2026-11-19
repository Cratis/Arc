// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Log messages for <see cref="McpToolAvailability"/>.
/// </summary>
internal static partial class McpToolAvailabilityLogging
{
    [LoggerMessage(LogLevel.Warning, "Availability of MCP tool '{Tool}' could not be determined; the tool is listed and a call is refused as a retryable error")]
    internal static partial void AvailabilityNotDecided(this ILogger logger, string tool, Exception exception);
}
