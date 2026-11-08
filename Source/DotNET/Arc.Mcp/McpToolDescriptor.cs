// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Describes a tool that may be served over MCP.
/// </summary>
/// <param name="Name">The stable, explicit snake_case tool name.</param>
/// <param name="Description">The required tool description.</param>
/// <param name="Kind">What the tool executes.</param>
/// <param name="Operation">The command type or <see cref="Queries.FullyQualifiedQueryName"/> the tool executes; null for custom tools.</param>
/// <param name="RequiredScopes">The scopes the verified caller must hold.</param>
/// <param name="Access">Whether the tool reads or writes.</param>
/// <param name="InputSchema">The JSON schema of the tool input, if known.</param>
/// <param name="Annotations">The behavioural hints advertised to clients.</param>
public sealed record McpToolDescriptor(
    string Name,
    string Description,
    McpToolKind Kind,
    object? Operation,
    IReadOnlyList<string> RequiredScopes,
    McpToolAccess Access,
    string? InputSchema,
    McpToolAnnotations Annotations);
