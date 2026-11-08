// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when two exposed tools share a name.
/// </summary>
/// <param name="name">The duplicated name.</param>
public class McpToolNameDuplicated(string name)
    : InvalidMcpConfiguration($"MCP tool name '{name}' is declared more than once.");
