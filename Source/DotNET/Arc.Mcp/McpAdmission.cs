// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Admits a request to the MCP transport after the capability gate: the caller must be verified, the body bounded, and
/// a <c>tools/call</c> must be covered by the caller's scopes before the MCP server reads it.
/// </summary>
/// <param name="options">The MCP options.</param>
/// <param name="ceiling">The factory of the tool ceiling.</param>
internal sealed class McpAdmission(IOptions<McpOptions> options, McpToolCeilingFactory ceiling)
{
    const string ToolsCall = "tools/call";

    /// <summary>
    /// Admits the request and hands it to the MCP transport, or refuses it.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <param name="next">The MCP transport.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Invoke(HttpContext context, RequestDelegate next)
    {
        var settings = options.Value;
        if (!McpOrigins.Allows(context.Request.Headers.Origin.ToArray(), settings))
        {
            await McpChallenge.Write(context, StatusCodes.Status403Forbidden, "origin_not_allowed");
            return;
        }

        if (McpCaller.From(context) is not { } caller)
        {
            await McpChallenge.Write(context, StatusCodes.Status401Unauthorized, "unauthorized", McpChallenge.Unauthorized(settings));
            return;
        }

        context.Items[CapabilityCallers.ItemKey] = caller.Capability with { Protocol = CapabilityProtocol.Mcp };

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.Headers.Allow = HttpMethods.Post;
            await McpChallenge.Write(context, StatusCodes.Status405MethodNotAllowed, "method_not_allowed");
            return;
        }

        var body = await ReadBounded(context, settings.MaxRequestBodySize);
        if (body is null)
        {
            await McpChallenge.Write(context, StatusCodes.Status413PayloadTooLarge, "request_too_large");
            return;
        }

        var refusal = Inspect(body, caller);
        if (refusal is not null)
        {
            await refusal(context, settings);
            return;
        }

        context.Request.Body = new MemoryStream(body, writable: false);
        context.Request.ContentLength = body.Length;
        await next(context);
    }

    static async Task<byte[]?> ReadBounded(HttpContext context, long limit)
    {
        if (context.Request.ContentLength > limit)
        {
            return null;
        }

        await using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await context.Request.Body.ReadAsync(chunk, context.RequestAborted)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return null;
            }
            await buffer.WriteAsync(chunk.AsMemory(0, read), context.RequestAborted);
        }

        return buffer.ToArray();
    }

    static Task InvalidRequest(HttpContext context, McpOptions settings) =>
        McpChallenge.Write(context, StatusCodes.Status400BadRequest, "invalid_request");

    static bool HasDuplicateProperties(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name) || HasDuplicateProperties(property.Value))
                    {
                        return true;
                    }
                }
                return false;

            case JsonValueKind.Array:
                return element.EnumerateArray().Any(HasDuplicateProperties);

            default:
                return false;
        }
    }

    Func<HttpContext, McpOptions, Task>? Inspect(byte[] body, McpCaller caller)
    {
        JsonDocument document;
        try
        {
            // Duplicate members are refused: the scope check below and the MCP server must read the same tool name.
            document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException)
        {
            return InvalidRequest;
        }

        using (document)
        {
            var root = document.RootElement;
            if (HasDuplicateProperties(root))
            {
                return InvalidRequest;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                // Batches were removed from MCP; refusing them keeps one request to one checked message.
                return InvalidRequest;
            }

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("method", out var method) ||
                method.ValueKind != JsonValueKind.String ||
                method.GetString() != ToolsCall ||
                !root.TryGetProperty("params", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object ||
                !parameters.TryGetProperty("name", out var name) ||
                name.ValueKind != JsonValueKind.String ||
                !ceiling.Ceiling.TryGet(name.GetString(), out var tool) ||
                caller.Holds(tool!.RequiredScopes))
            {
                return null;
            }

            var scopes = tool.RequiredScopes;
            return (context, settings) => McpChallenge.Write(
                context,
                StatusCodes.Status403Forbidden,
                "insufficient_scope",
                McpChallenge.InsufficientScope(settings, scopes, caller.CanStepUp));
        }
    }
}
