// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when an exposed tool has no description.
/// </summary>
/// <param name="name">The tool name.</param>
public class McpToolDescriptionMissing(string name)
    : InvalidMcpConfiguration($"MCP tool '{name}' has no description.");
