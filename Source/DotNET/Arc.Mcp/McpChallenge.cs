// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Writes the MCP authorization challenges and refusals: JSON bodies, never HTML, never a redirect.
/// </summary>
/// <remarks>
/// Every value in a challenge comes from configuration. Nothing is derived from the request host or forwarding headers.
/// </remarks>
internal static class McpChallenge
{
    /// <summary>
    /// The well-known prefix of protected resource metadata (RFC 9728).
    /// </summary>
    internal const string MetadataPrefix = "/.well-known/oauth-protected-resource";

    /// <summary>
    /// Gets the path the protected resource metadata is served at for the configured MCP path.
    /// </summary>
    /// <param name="options">The MCP options.</param>
    /// <returns>The metadata path.</returns>
    internal static string MetadataPath(McpOptions options) => MetadataPrefix + options.Path.TrimEnd('/');

    /// <summary>
    /// Gets whether the profile advertises OAuth protected resource metadata.
    /// </summary>
    /// <param name="options">The MCP options.</param>
    /// <returns>True for the OAuth and combined profiles.</returns>
    internal static bool AdvertisesMetadata(McpOptions options) => options.Profile != McpProfile.PatOnly;

    /// <summary>
    /// Builds the challenge for a request without an admitted credential.
    /// </summary>
    /// <param name="options">The MCP options.</param>
    /// <returns>The <c>WWW-Authenticate</c> value.</returns>
    internal static string Unauthorized(McpOptions options) =>
        AdvertisesMetadata(options)
            ? $"Bearer resource_metadata=\"{MetadataUrl(options)}\""
            : Realm(options);

    /// <summary>
    /// Builds the challenge for a caller that lacks scopes a tool requires.
    /// </summary>
    /// <param name="options">The MCP options.</param>
    /// <param name="scopes">The scopes the tool requires.</param>
    /// <param name="stepUp">Whether the caller can step up through OAuth, which adds the metadata location.</param>
    /// <returns>The <c>WWW-Authenticate</c> value.</returns>
    internal static string InsufficientScope(McpOptions options, IEnumerable<string> scopes, bool stepUp)
    {
        var challenge = $"Bearer error=\"insufficient_scope\", scope=\"{string.Join(' ', scopes)}\"";
        return stepUp && AdvertisesMetadata(options)
            ? $"{challenge}, resource_metadata=\"{MetadataUrl(options)}\""
            : challenge;
    }

    /// <summary>
    /// Writes a JSON refusal.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <param name="status">The status code.</param>
    /// <param name="error">The error code written to the body.</param>
    /// <param name="challenge">The optional <c>WWW-Authenticate</c> value.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Write(HttpContext context, int status, string error, string? challenge = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        context.Response.Headers.Location = default;
        if (challenge is not null)
        {
            context.Response.Headers.WWWAuthenticate = challenge;
        }

        await using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
        writer.WriteStartObject();
        writer.WriteString("error", error);
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);
        await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
    }

    static string MetadataUrl(McpOptions options) => $"{options.PublicOrigin!.TrimEnd('/')}{MetadataPath(options)}";

    static string Realm(McpOptions options) =>
        string.IsNullOrWhiteSpace(options.ApplicationName) ? "Bearer" : $"Bearer realm=\"{options.ApplicationName}\"";
}
