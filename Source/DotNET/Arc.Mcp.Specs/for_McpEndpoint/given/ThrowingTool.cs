// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A curated read tool that always fails with an internal detail in its message.
/// </summary>
public class ThrowingTool : IMcpToolSource
{
    /// <summary>
    /// The detail the failure carries, which must never reach a caller.
    /// </summary>
    public const string Detail = "server-internal-detail-4711";

    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        new McpToolDescriptor(
            "throwing_tool",
            "Always fails.",
            McpToolKind.Custom,
            null,
            ["app:read"],
            McpToolAccess.Read,
            """{"type":"object"}""",
            McpToolAnnotations.ForRead)
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => throw new InvalidOperationException(Detail);
}
