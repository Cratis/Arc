// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Log messages for <see cref="McpToolDispatcher"/>.
/// </summary>
internal static partial class McpToolDispatcherLogging
{
    [LoggerMessage(LogLevel.Warning, "MCP tool '{Tool}' failed")]
    internal static partial void ToolFailed(this ILogger<McpToolDispatcher> logger, string tool, Exception exception);
}
