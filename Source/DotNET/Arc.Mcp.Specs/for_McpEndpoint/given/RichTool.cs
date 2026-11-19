// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Curated read tools returning structured results and resource links, well formed or not.
/// </summary>
public class RichTool : IMcpToolSource
{
    const string Schema = """{"type":"object","properties":{"count":{"type":"integer"}}}""";

    /// <inheritdoc/>
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        McpToolDescriptor.Custom("count_notes", "Counts notes.", ["app:read"], McpToolAccess.Read) with { Title = "Count notes", OutputSchema = Schema },
        McpToolDescriptor.Custom("missing_structure", "Declares a schema but returns no structure.", ["app:read"], McpToolAccess.Read) with { OutputSchema = Schema },
        McpToolDescriptor.Custom("array_structure", "Returns a structure that is not an object.", ["app:read"], McpToolAccess.Read),
        McpToolDescriptor.Custom("relative_link", "Returns a link without an absolute URI.", ["app:read"], McpToolAccess.Read),
        McpToolDescriptor.Custom("null_links", "Returns null resource links.", ["app:read"], McpToolAccess.Read)
    ];

    /// <inheritdoc/>
    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) => ValueTask.FromResult(invocation.Tool.Name switch
    {
        "count_notes" => McpToolResult.Success("""{"count":2}""") with
        {
            StructuredContent = Structured("""{"count":2}"""),
            ResourceLinks = [new McpResourceLink("notes://notes/1", "First note") { Title = "First", MimeType = "text/plain" }]
        },
        "array_structure" => McpToolResult.Success("[]") with { StructuredContent = Structured("[]") },
        "relative_link" => McpToolResult.Success("linked") with { ResourceLinks = [new McpResourceLink("notes/1", "Relative")] },
        "null_links" => McpToolResult.Success("no links") with { ResourceLinks = null! },
        _ => McpToolResult.Success("no structure")
    });

    static JsonElement Structured(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
