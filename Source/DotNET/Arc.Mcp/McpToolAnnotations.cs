// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the behavioural hints a tool advertises to MCP clients.
/// </summary>
/// <param name="ReadOnly">Whether the tool does not modify state.</param>
/// <param name="Destructive">Whether the tool may perform destructive updates.</param>
/// <param name="Idempotent">Whether repeated calls with the same arguments have no additional effect.</param>
/// <param name="OpenWorld">Whether the tool interacts with an open world of external entities. Always false for Arc operations.</param>
public sealed record McpToolAnnotations(bool ReadOnly, bool Destructive, bool Idempotent, bool OpenWorld = false)
{
    /// <summary>
    /// Gets the annotations of a read tool.
    /// </summary>
    public static McpToolAnnotations ForRead { get; } = new(true, false, true);

    /// <summary>
    /// Gets the conservative annotations of a write tool.
    /// </summary>
    public static McpToolAnnotations ForWrite { get; } = new(false, true, false);
}
