// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Adds the tools of the frozen ceiling to the MCP server, next to the tools the application registered on the server.
/// </summary>
/// <remarks>
/// Runs as post-configuration, after the server's own setup. Tools the application registered directly on the MCP server
/// are kept, unless <see cref="McpOptions.ExclusiveTools"/> is set, but are not part of the ceiling and are not governed
/// by it. A server tool whose name matches a ceiling tool, ignoring case, fails startup so it can never shadow one.
/// </remarks>
/// <param name="ceiling">The factory of the tool ceiling.</param>
/// <param name="dispatcher">The dispatcher every tool calls through.</param>
/// <param name="mcp">The MCP options.</param>
internal sealed class McpServerToolsConfiguration(McpToolCeilingFactory ceiling, McpToolDispatcher dispatcher, IOptions<McpOptions> mcp) : IPostConfigureOptions<McpServerOptions>
{
    /// <inheritdoc/>
    /// <exception cref="McpToolNameTakenByServerTool">A tool registered on the server has the name of a ceiling tool.</exception>
    public void PostConfigure(string? name, McpServerOptions options)
    {
        var tools = new McpServerPrimitiveCollection<McpServerTool>(StringComparer.Ordinal);
        if (mcp.Value.ExclusiveTools)
        {
            // Nothing but the ceiling is listed or callable: handlers that would serve other tools are removed too.
            options.Handlers.ListToolsHandler = null;
            options.Handlers.CallToolHandler = null;
        }
        else
        {
            foreach (var serverTool in options.ToolCollection ?? [])
            {
                var serverName = serverTool.ProtocolTool.Name;
                if (ceiling.Ceiling.Tools.Any(_ => string.Equals(_.Name, serverName, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new McpToolNameTakenByServerTool(serverName);
                }

                tools.Add(serverTool);
            }
        }

        foreach (var tool in ceiling.Ceiling.Tools)
        {
            tools.Add(new ArcMcpTool(tool, dispatcher));
        }

        options.ToolCollection = tools;
    }
}
