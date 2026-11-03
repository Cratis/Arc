// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Decides whether the origin of a WebSocket connection request is trusted.
/// </summary>
internal static class WebSocketOrigins
{
    /// <summary>
    /// The configuration value that accepts every origin.
    /// </summary>
    internal const string Wildcard = "*";

    /// <summary>
    /// Normalizes an origin to <c>scheme://host:port</c> in lower case, with the effective port always written out.
    /// </summary>
    /// <param name="value">The origin to normalize.</param>
    /// <param name="normalized">The normalized origin when successful.</param>
    /// <returns>True when <paramref name="value"/> is a plain http or https origin; false otherwise.</returns>
    internal static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Any(c => char.IsWhiteSpace(c) || c == ','))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            (uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/") ||
            value.EndsWith('?') ||
            value.EndsWith('#'))
        {
            return false;
        }

        normalized = $"{uri.Scheme}://{uri.IdnHost.ToLowerInvariant()}:{uri.Port}";
        return true;
    }

    /// <summary>
    /// Determines whether a connection request with the given origin is trusted.
    /// </summary>
    /// <param name="origin">The value of the <c>Origin</c> header, or null when the header is absent.</param>
    /// <param name="hostHeader">The value of the <c>Host</c> header, optionally including a port.</param>
    /// <param name="isHttps">Whether the request arrived over a secure connection.</param>
    /// <param name="allowedOrigins">The configured allowed origins.</param>
    /// <returns>True when the origin is absent, configured, or the same origin as the request; false otherwise.</returns>
    internal static bool IsTrusted(string? origin, string? hostHeader, bool isHttps, IEnumerable<string> allowedOrigins)
    {
        var configured = allowedOrigins.Where(_ => _ is not null).Select(_ => _.Trim()).ToArray();
        if (configured.Contains(Wildcard))
        {
            return true;
        }

        if (origin is null)
        {
            return true;
        }

        if (!TryNormalize(origin, out var normalizedOrigin))
        {
            return false;
        }

        if (configured.Any(entry => TryNormalize(entry, out var normalizedEntry) && normalizedEntry == normalizedOrigin))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return false;
        }

        var scheme = isHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        return TryNormalize($"{scheme}://{hostHeader.Trim()}", out var normalizedRequest) && normalizedRequest == normalizedOrigin;
    }
}
