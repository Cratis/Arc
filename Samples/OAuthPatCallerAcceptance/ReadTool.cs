// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp;

namespace OAuthPatCallerAcceptance;

/// <summary>The same read permission used by the REST endpoint.</summary>
/// <param name="store">The sample's dispatch counter.</param>
public sealed class ReadTool(SampleIdentityStore store) : IMcpToolSource
{
    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
        [new("read", "Reads as the checked person.", McpToolKind.Custom, null, ["read"], McpToolAccess.Read, """{"type":"object","properties":{}}""", McpToolAnnotations.ForRead)];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation)
    {
        store.Dispatched();
        return ValueTask.FromResult(McpToolResult.Success($"{invocation.Caller.Person}|{invocation.Caller.CredentialId}|{invocation.Caller.Tenant}"));
    }
}
