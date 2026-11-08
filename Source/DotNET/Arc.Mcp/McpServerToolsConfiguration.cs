// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Sets the tools of the MCP server: the frozen ceiling, plus the tools the application registered on the server when
/// <see cref="McpOptions.ExclusiveTools"/> is turned off.
/// </summary>
/// <remarks>
/// Runs as post-configuration, after the server's own setup. By default only the ceiling is served. With
/// <see cref="McpOptions.ExclusiveTools"/> off, tools the application registered directly on the MCP server are kept, but
/// are not part of the ceiling and are not governed by it; a warning is logged. A server tool whose name matches a ceiling
/// tool, ignoring case, fails startup so it can never shadow one.
/// </remarks>
/// <param name="ceiling">The factory of the tool ceiling.</param>
/// <param name="dispatcher">The dispatcher every tool calls through.</param>
/// <param name="mcp">The MCP options.</param>
/// <param name="logger">The logger.</param>
internal sealed class McpServerToolsConfiguration(
    McpToolCeilingFactory ceiling,
    McpToolDispatcher dispatcher,
    IOptions<McpOptions> mcp,
    ILogger<McpServerToolsConfiguration> logger) : IPostConfigureOptions<McpServerOptions>
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

            logger.UngovernedServerToolsServed(string.Join(", ", tools.PrimitiveNames));
        }

        foreach (var tool in ceiling.Ceiling.Tools)
        {
            tools.Add(new ArcMcpTool(tool, dispatcher));
        }

        options.ToolCollection = tools;
    }
}
