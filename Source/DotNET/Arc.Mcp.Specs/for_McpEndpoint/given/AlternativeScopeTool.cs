// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A curated read tool callable with the read scope or with both of two other scopes.
/// </summary>
public class AlternativeScopeTool : IMcpToolSource
{
    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        McpToolDescriptor.Custom("audit_log", "Reads the audit log.", ["app:read"], McpToolAccess.Read) with
        {
            AlternativeScopes = [["audit:read", "audit:export"]]
        }
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("audited"));
}
