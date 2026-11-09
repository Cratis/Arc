// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Log messages for <see cref="McpServerToolsConfiguration"/>.
/// </summary>
internal static partial class McpServerToolsConfigurationLogging
{
    [LoggerMessage(LogLevel.Warning, "MCP ExclusiveTools is off: tools registered directly on the MCP server, and any custom tool list or call handlers, are served to every admitted caller without scope checks or tool authorizers. Server tools: [{Tools}]")]
    internal static partial void UngovernedServerToolsServed(this ILogger<McpServerToolsConfiguration> logger, string tools);
}
