// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when an exposed tool would require no scope.
/// </summary>
/// <param name="name">The tool name.</param>
public class McpToolHasNoScope(string name)
    : InvalidMcpConfiguration($"MCP tool '{name}' requires no scope. Configure McpOptions.ApplicationName, ReadScope and WriteScope.");
