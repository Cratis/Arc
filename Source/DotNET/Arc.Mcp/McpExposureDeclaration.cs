// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents one explicit declaration that an operation is exposed over MCP.
/// </summary>
/// <param name="Kind">Whether the operation is a command or a query.</param>
/// <param name="Operation">The command type, or the fully qualified query name as a string.</param>
/// <param name="Name">The tool name.</param>
/// <param name="Description">The tool description.</param>
public sealed record McpExposureDeclaration(McpToolKind Kind, object Operation, string Name, string? Description);
