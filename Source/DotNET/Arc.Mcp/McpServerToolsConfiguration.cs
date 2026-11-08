// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Gives the MCP server exactly the tools of the frozen ceiling; nothing else is callable.
/// </summary>
/// <remarks>
/// Runs as post-configuration, after the server's own setup, so tools registered any other way are replaced.
/// </remarks>
/// <param name="ceiling">The factory of the tool ceiling.</param>
/// <param name="dispatcher">The dispatcher every tool calls through.</param>
internal sealed class McpServerToolsConfiguration(McpToolCeilingFactory ceiling, McpToolDispatcher dispatcher) : IPostConfigureOptions<McpServerOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, McpServerOptions options)
    {
        var tools = new McpServerPrimitiveCollection<McpServerTool>(StringComparer.Ordinal);
        foreach (var tool in ceiling.Ceiling.Tools)
        {
            tools.Add(new ArcMcpTool(tool, dispatcher));
        }

        options.ToolCollection = tools;
    }
}
