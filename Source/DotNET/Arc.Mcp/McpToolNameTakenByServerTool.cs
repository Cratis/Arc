// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when a tool registered directly on the MCP server has the name of a tool in the ceiling.
/// </summary>
/// <param name="name">The name both tools share.</param>
public class McpToolNameTakenByServerTool(string name)
    : InvalidMcpConfiguration($"MCP tool name '{name}' is used both by a tool registered on the MCP server and by an exposed tool.");
