// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Serves the protected resource metadata (RFC 9728) of the MCP endpoint.
/// </summary>
/// <remarks>
/// Every value comes from <see cref="McpOptions"/>; the request host and forwarding headers are never read. The
/// PAT-only profile has no OAuth metadata and answers 404.
/// </remarks>
internal static class McpProtectedResourceMetadata
{
    /// <summary>
    /// Writes the metadata for the request.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Serve(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<McpOptions>>().Value;
        if (!McpChallenge.AdvertisesMetadata(options))
        {
            await McpChallenge.Write(context, StatusCodes.Status404NotFound, "not_found");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        await using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
        writer.WriteStartObject();
        writer.WriteString("resource", options.Resource);
        writer.WriteStartArray("authorization_servers");
        foreach (var server in options.AuthorizationServers)
        {
            writer.WriteStringValue(server);
        }
        writer.WriteEndArray();
        writer.WriteStartArray("scopes_supported");
        foreach (var scope in ScopesSupported(options))
        {
            writer.WriteStringValue(scope);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);
        await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
    }

    static IEnumerable<string> ScopesSupported(McpOptions options) =>
        options.ScopesSupported.Count > 0
            ? options.ScopesSupported
            : new[] { options.ReadScope }.Concat(options.ConfiguredWriteScopes()).OfType<string>().Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct(StringComparer.Ordinal);
}
