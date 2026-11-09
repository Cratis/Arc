// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A response from the host, with the JSON-RPC message of an event stream unwrapped.
/// </summary>
/// <param name="Status">The HTTP status.</param>
/// <param name="Challenge">The WWW-Authenticate header.</param>
/// <param name="ContentType">The content type.</param>
/// <param name="Body">The raw body.</param>
public sealed record McpResponse(HttpStatusCode Status, string Challenge, string? ContentType, string Body)
{
    /// <summary>
    /// Gets the JSON message: the last <c>data:</c> line of an event stream, or the body.
    /// </summary>
    public JsonElement Message
    {
        get
        {
            var data = Body.Split('\n').Where(_ => _.StartsWith("data:", StringComparison.Ordinal)).Select(_ => _[5..].Trim()).LastOrDefault() ?? Body;
            using var document = JsonDocument.Parse(data);
            return document.RootElement.Clone();
        }
    }

    /// <summary>
    /// Gets whether the message is a JSON-RPC protocol error.
    /// </summary>
    public bool IsProtocolError => Message.TryGetProperty("error", out _);

    /// <summary>
    /// Gets whether the message is a tool result that reports an error.
    /// </summary>
    public bool IsToolError => Message.GetProperty("result").TryGetProperty("isError", out var isError) && isError.GetBoolean();

    /// <summary>
    /// Gets the text of the tool result.
    /// </summary>
    public string ToolText => Message.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;

    /// <summary>
    /// Gets the names of the listed tools.
    /// </summary>
    public IEnumerable<string> ToolNames => Message.GetProperty("result").GetProperty("tools").EnumerateArray().Select(_ => _.GetProperty("name").GetString()!).ToArray();

    /// <summary>
    /// Gets the error code of a refusal body.
    /// </summary>
    public string RefusalError
    {
        get
        {
            using var document = JsonDocument.Parse(Body);
            return document.RootElement.GetProperty("error").GetString()!;
        }
    }
}
