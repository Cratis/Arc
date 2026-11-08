// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when the output schema of a tool is not a JSON schema describing an object.
/// </summary>
/// <param name="name">The tool name.</param>
public class McpToolOutputSchemaInvalid(string name)
    : InvalidMcpConfiguration($"MCP tool '{name}' has an output schema that is not a JSON schema of type 'object'.");
