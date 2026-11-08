// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when an exposed operation is not a command or query known to Arc.
/// </summary>
/// <param name="name">The tool name.</param>
/// <param name="operation">The unknown operation.</param>
public class McpExposedOperationUnknown(string name, object operation)
    : InvalidMcpConfiguration($"MCP tool '{name}' exposes '{operation}', which is not a command or query known to Arc.");
