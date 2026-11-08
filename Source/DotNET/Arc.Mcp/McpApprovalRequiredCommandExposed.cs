// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when a command that requires approval is exposed over MCP.
/// </summary>
/// <param name="name">The tool name.</param>
/// <param name="commandType">The command type.</param>
public class McpApprovalRequiredCommandExposed(string name, Type commandType)
    : InvalidMcpConfiguration($"MCP tool '{name}' exposes '{commandType}', which requires approval. Approval-required commands are never exposed over MCP.");
