// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Decides whether a browser <c>Origin</c> header may call the MCP endpoint, and validates configured origins and URLs.
/// </summary>
internal static class McpOrigins
{
    /// <summary>
    /// Gets whether a request with the given <c>Origin</c> values is allowed. No <c>Origin</c> is allowed: it is not a browser.
    /// </summary>
    /// <param name="origins">The <c>Origin</c> header values of the request.</param>
    /// <param name="options">The MCP options.</param>
    /// <returns>True if the request may proceed.</returns>
    internal static bool Allows(IReadOnlyList<string?> origins, McpOptions options)
    {
        if (origins.Count == 0)
        {
            return true;
        }

        if (origins.Count > 1 || !IsOrigin(origins[0]) || !TryNormalize(origins[0], out var origin))
        {
            return false;
        }

        return Allowed(options).Contains(origin);
    }

    /// <summary>
    /// Gets whether the value is an acceptable public URL: absolute, https (http only for loopback), no fragment, no user info.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="uri">The parsed URI.</param>
    /// <returns>True if acceptable.</returns>
    internal static bool IsPublicUrl(string? value, out Uri uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            uri = null!;
            return false;
        }

        uri = parsed;
        return (parsed.Scheme == Uri.UriSchemeHttps || (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback)) &&
            string.IsNullOrEmpty(parsed.Fragment) &&
            !value!.Contains('#') &&
            string.IsNullOrEmpty(parsed.UserInfo);
    }

    /// <summary>
    /// Gets whether the value is an acceptable origin: a public URL with no path, query or fragment.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if acceptable.</returns>
    internal static bool IsOrigin(string? value) =>
        IsPublicUrl(value, out var uri) && (uri.AbsolutePath == "/" || uri.AbsolutePath.Length == 0) && string.IsNullOrEmpty(uri.Query);

    /// <summary>
    /// Gets whether the resource is the public origin plus the MCP path.
    /// </summary>
    /// <param name="options">The MCP options.</param>
    /// <returns>True if consistent.</returns>
    internal static bool ResourceMatches(McpOptions options) =>
        IsPublicUrl(options.Resource, out var resource) &&
        Uri.TryCreate(options.PublicOrigin, UriKind.Absolute, out var origin) &&
        string.IsNullOrEmpty(resource.Query) &&
        Uri.Compare(resource, origin, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0 &&
        string.Equals(resource.AbsolutePath.TrimEnd('/'), options.Path.TrimEnd('/'), StringComparison.Ordinal);

    static HashSet<string> Allowed(McpOptions options)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var configured in options.AllowedOrigins.Append(options.PublicOrigin))
        {
            if (IsOrigin(configured) && TryNormalize(configured, out var origin))
            {
                allowed.Add(origin);
            }
        }

        return allowed;
    }

    static bool TryNormalize(string? value, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        origin = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
        return true;
    }
}
