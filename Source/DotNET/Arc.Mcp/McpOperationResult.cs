// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Arc.Mcp;

/// <summary>Pairs the existing serialized Arc envelope with its identical structured representation.</summary>
internal static class McpOperationResult
{
    /// <summary>Creates a tool result from a payload.</summary>
    /// <param name="payload">The payload to serialize.</param>
    /// <param name="isError">Whether the result is an error.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The tool result.</returns>
    internal static McpToolResult From(object payload, bool isError, JsonSerializerOptions options)
    {
        var text = JsonSerializer.Serialize(payload, options);
        using var document = JsonDocument.Parse(text);
        return new McpToolResult(text, isError) { StructuredContent = document.RootElement.Clone() };
    }

    /// <summary>Builds the output schema of a command.</summary>
    /// <param name="options">The serializer options.</param>
    /// <returns>The schema as JSON.</returns>
    internal static string CommandSchema(JsonSerializerOptions options) => Schema(options, "response");

    /// <summary>Builds the output schema of a query.</summary>
    /// <param name="options">The serializer options.</param>
    /// <returns>The schema as JSON.</returns>
    internal static string QuerySchema(JsonSerializerOptions options) => Schema(options, "data", "paging");

    static string Schema(JsonSerializerOptions options, params string[] fields)
    {
        var outcome = options.PropertyNamingPolicy?.ConvertName("outcome") ?? "outcome";
        var properties = new JsonObject { [outcome] = new JsonObject { ["type"] = "object" } };
        foreach (var field in fields)
        {
            properties[options.PropertyNamingPolicy?.ConvertName(field) ?? field] = new JsonObject();
        }
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(outcome)
        }.ToJsonString();
    }
}
