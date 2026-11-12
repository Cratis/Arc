// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents a tool from the <see cref="McpToolCatalog"/> registered with the MCP server.
/// </summary>
/// <param name="descriptor">The tool from the catalog.</param>
/// <param name="dispatcher">The dispatcher that runs the tool as the verified caller.</param>
internal sealed class ArcMcpTool(McpToolDescriptor descriptor, McpToolDispatcher dispatcher) : McpServerTool
{
    /// <summary>
    /// Gets the tool from the catalog.
    /// </summary>
    public McpToolDescriptor Descriptor => descriptor;

    /// <inheritdoc/>
    public override Tool ProtocolTool { get; } = new()
    {
        Name = descriptor.Name,
        Title = descriptor.Title,
        Description = descriptor.Description,
        InputSchema = ParseSchema(descriptor.InputSchema ?? """{"type":"object"}"""),
        OutputSchema = descriptor.OutputSchema is null ? null : ParseSchema(descriptor.OutputSchema),
        Annotations = new ToolAnnotations
        {
            ReadOnlyHint = descriptor.Annotations.ReadOnly,
            DestructiveHint = descriptor.Annotations.Destructive,
            IdempotentHint = descriptor.Annotations.Idempotent,
            OpenWorldHint = descriptor.Annotations.OpenWorld
        }
    };

    /// <inheritdoc/>
    public override IReadOnlyList<object> Metadata { get; } = [];

    /// <inheritdoc/>
    public override ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default) =>
        dispatcher.Dispatch(descriptor, request.Params?.Arguments, cancellationToken, request);

    static JsonElement ParseSchema(string schema)
    {
        using var document = JsonDocument.Parse(schema);
        return document.RootElement.Clone();
    }
}
