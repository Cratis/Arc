// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A curated read tool reporting what it sees of the protocol request.
/// </summary>
public class RequestContextTool : IMcpToolSource
{
    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        McpToolDescriptor.Custom("request_context", "Reports the protocol request.", ["app:read"], McpToolAccess.Read)
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) =>
        ValueTask.FromResult(McpToolResult.Success($"{invocation.Request?.Params?.Name}|{invocation.Request?.Server is not null}"));
}
