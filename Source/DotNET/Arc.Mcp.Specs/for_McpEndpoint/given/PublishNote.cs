// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A curated write tool requiring a write scope other than the default one, or the default write scope with an audit scope.
/// </summary>
public class PublishNote : IMcpToolSource
{
    /// <summary>
    /// The write scope the tool requires.
    /// </summary>
    public const string PublishScope = "notes:publish";

    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        McpToolDescriptor.Custom("publish_note", "Publishes a note.", ["app:read", PublishScope], McpToolAccess.Write) with
        {
            AlternativeScopes = [["audit:read", "app:write"]]
        }
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(McpToolResult.Success("published"));
}
