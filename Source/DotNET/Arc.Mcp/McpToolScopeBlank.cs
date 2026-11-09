// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when an exposed operation declares a blank required scope.
/// </summary>
/// <param name="name">The tool name.</param>
public class McpToolScopeBlank(string name)
    : InvalidMcpConfiguration($"MCP tool '{name}' declares a blank required scope.");
