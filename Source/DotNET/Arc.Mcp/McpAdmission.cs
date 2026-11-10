// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Admits a request to the MCP transport after the capability gate: the caller must be verified, the body bounded, and
/// a <c>tools/call</c> of a catalog tool must be covered by the caller's scopes and allowed by every
/// <see cref="IMcpToolAuthorizer"/> before the MCP server reads it.
/// </summary>
/// <param name="options">The MCP options.</param>
/// <param name="catalog">The factory of the tool catalog.</param>
/// <param name="logger">The admission diagnostics logger.</param>
/// <param name="audit">The identifier-only call audit, when installed.</param>
internal sealed class McpAdmission(IOptions<McpOptions> options, McpToolCatalogFactory catalog, ILogger<McpAdmission> logger, McpCallAudit? audit = null)
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
            logger.OriginRefused(context.Request.Headers.Origin.ToArray());
            await McpChallenge.Write(context, StatusCodes.Status403Forbidden, "origin_not_allowed");
            return;
        }

        if (McpCaller.From(context) is not { } caller || !AcceptsCredential(settings.Profile, caller.Verified.CredentialKind))
        {
            // A verified caller whose credential kind the profile does not accept is refused exactly like no caller.
            await McpChallenge.Write(context, StatusCodes.Status401Unauthorized, "unauthorized", McpChallenge.Unauthorized(settings));
            return;
        }

        CapabilityRequestContext.SetProtocol(context.Items, CapabilityProtocol.Mcp);

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

        var (refusal, tool) = Inspect(body, caller);
        if (refusal is not null)
        {
            RecordScopeRefusal(tool, caller);
            await refusal(context, settings);
            return;
        }

        if (!await AllowsTool(tool, caller, context))
        {
            audit?.Record(tool!, caller, "not_allowed");
            await McpChallenge.Write(context, StatusCodes.Status403Forbidden, "forbidden");
            return;
        }

        context.Request.Body = new MemoryStream(body, writable: false);
        context.Request.ContentLength = body.Length;
        await next(context);
    }

    static ValueTask<bool> AllowsTool(McpToolDescriptor? tool, McpCaller caller, HttpContext context) =>
        tool is null ? ValueTask.FromResult(true) : McpToolAuthorizers.Allow(tool, caller, context.RequestServices, context.RequestAborted);

    static bool AcceptsCredential(McpProfile profile, CallerCredentialKind kind) => profile switch
    {
        McpProfile.PatOnly => kind == CallerCredentialKind.PersonalAccessToken,
        McpProfile.OAuthOnly => kind == CallerCredentialKind.OAuthGrant,
        McpProfile.Combined => kind is CallerCredentialKind.PersonalAccessToken or CallerCredentialKind.OAuthGrant,
        _ => false
    };

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

    static bool IsToolCall(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty("method", out var method) &&
        method.ValueKind == JsonValueKind.String &&
        method.GetString() == ToolsCall;

    void RecordScopeRefusal(McpToolDescriptor? tool, McpCaller caller)
    {
        if (tool is not null)
        {
            audit?.Record(tool, caller, "insufficient_scope");
        }
    }

    (Func<HttpContext, McpOptions, Task>? Refusal, McpToolDescriptor? Tool) Inspect(byte[] body, McpCaller caller)
    {
        JsonDocument document;
        try
        {
            // Duplicate members are refused: the scope check below and the MCP server must read the same tool name.
            document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException)
        {
            return (InvalidRequest, null);
        }

        using (document)
        {
            var root = document.RootElement;
            if (HasDuplicateProperties(root))
            {
                return (InvalidRequest, null);
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                // Batches were removed from MCP; refusing them keeps one request to one checked message.
                return (InvalidRequest, null);
            }

            if (!IsToolCall(root) || !TryFindTool(root, out var tool))
            {
                return (null, null);
            }

            if (caller.HoldsScopesFor(tool!))
            {
                return (null, tool);
            }

            var scopes = tool!.RequiredScopes;
            return ((context, settings) => McpChallenge.Write(
                context,
                StatusCodes.Status403Forbidden,
                "insufficient_scope",
                McpChallenge.InsufficientScope(settings, scopes, caller.CanStepUp)), tool);
        }
    }

    bool TryFindTool(JsonElement root, out McpToolDescriptor? tool)
    {
        tool = null;
        return root.TryGetProperty("params", out var parameters) &&
            parameters.ValueKind == JsonValueKind.Object &&
            parameters.TryGetProperty("name", out var name) &&
            name.ValueKind == JsonValueKind.String &&
            catalog.Catalog.TryGet(name.GetString(), out tool);
    }
}
