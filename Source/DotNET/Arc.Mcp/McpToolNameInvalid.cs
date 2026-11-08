// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when a tool name is not a stable snake_case name.
/// </summary>
/// <param name="name">The offending name.</param>
public class McpToolNameInvalid(string? name)
    : InvalidMcpConfiguration($"MCP tool name '{name}' is invalid. Names must match ^[a-z][a-z0-9_]{{0,63}}$.");
