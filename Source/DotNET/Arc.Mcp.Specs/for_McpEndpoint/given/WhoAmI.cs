// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A curated read tool reporting the caller it runs as.
/// </summary>
public class WhoAmI : IMcpToolSource
{
    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        new McpToolDescriptor(
            "who_am_i",
            "Reports the caller.",
            McpToolKind.Custom,
            null,
            ["app:read"],
            McpToolAccess.Read,
            """{"type":"object","properties":{"tenant":{"type":"string"}}}""",
            McpToolAnnotations.ForRead)
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation)
    {
        invocation.Services.GetRequiredService<McpProbe>().Record("who_am_i", null, invocation.Caller);
        return ValueTask.FromResult(McpToolResult.Success($"{invocation.Caller.Person}|{invocation.Caller.Tenant}|{invocation.Caller.Protocol}"));
    }
}
