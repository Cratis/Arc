// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Builds the bounded, length-framed v2 signing input from a raw snapshot.</summary>
internal static class ForwardedIdentityV2Canonicalization
{
    const string MethodToken = "!#$%&'*+-.^_`|~";

    /// <summary>Tests coverage before validating the raw header name.</summary>
    /// <param name="name">The raw header name.</param>
    /// <param name="tenantHeader">The configured tenant header.</param>
    /// <returns>Whether the header must be signed.</returns>
    internal static bool IsCovered(string name, string? tenantHeader) => ForwardedIdentityV2Headers.IsCovered(name, tenantHeader);

    /// <summary>Canonicalizes the request with explicit configuration, never inferred host metadata.</summary>
    /// <param name="settings">The installation settings.</param>
    /// <param name="request">The immutable raw snapshot.</param>
    /// <param name="parts">The parsed wire envelope components.</param>
    /// <returns>The canonical bytes and covered fields.</returns>
    internal static (byte[] Bytes, SortedDictionary<string, string> Headers) Create(ForwardedIdentityV2Settings settings, ForwardedIdentityV2Request request, string[] parts)
    {
        CheckAudience(settings.Audience);
        if (request.Method.Length == 0 || request.Method.Any(c => !(c is (>= 'A' and <= 'Z') or (>= '0' and <= '9')) && !MethodToken.Contains(c))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Request);
        var path = CanonicalPath(request.RawTarget);
        var headers = CoveredHeaders(request, settings.TenantHeader);
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes("Cratis.AuthProxy.ForwardedIdentity.v2\n"));
        foreach (var value in new[] { settings.Audience, parts[1], parts[2], parts[3], request.Method, path, headers.Count.ToString(CultureInfo.InvariantCulture) }) WriteFrame(stream, value);
        foreach (var entry in headers)
        {
            WriteFrame(stream, entry.Key);
            WriteFrame(stream, entry.Value);
        }
        return (stream.ToArray(), headers);
    }

    static void CheckAudience(string audience)
    {
        if (audience.Length is < 1 or > 64 || audience[0] is < 'a' or > 'z' || audience.Any(c => !(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')) || audience.Split('-').Any(segment => segment.Length == 0)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Request);
    }

    static SortedDictionary<string, string> CoveredHeaders(ForwardedIdentityV2Request request, string? tenantHeader)
    {
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in request.Headers)
        {
            if (!IsCovered(header.Name, tenantHeader)) continue;
            var name = header.Name.ToLowerInvariant();
            if (header.Name.Length == 0 || header.Name.Any(c => !char.IsAsciiLetterOrDigit(c) && !MethodToken.Contains(c))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Headers);
            var value = header.Value.Trim(' ', '\t');
            if (value.Any(c => c < 32 || c > 126) || !headers.TryAdd(name, value)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Headers);
        }
        if (headers.Count > 64) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Bounds);
        foreach (var required in new[] { "x-ms-client-principal", "x-ms-client-principal-id", "x-ms-client-principal-name" })
        {
            if (!headers.TryGetValue(required, out var value) || value.Length == 0) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Headers);
        }
        return headers;
    }

    static string CanonicalPath(string target)
    {
        if (!target.StartsWith('/') || target.Any(c => c < 33 || c > 126 || c == '#')) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Request);
        var query = target.IndexOf('?');
        var path = (query < 0 ? target : target[..query]).ToCharArray();
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] != '%') continue;
            if (i + 2 >= path.Length || !Uri.IsHexDigit(path[i + 1]) || !Uri.IsHexDigit(path[i + 2])) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Request);
            path[++i] = char.ToUpperInvariant(path[i]);
            path[++i] = char.ToUpperInvariant(path[i]);
        }
        return new(path);
    }

    static void WriteFrame(MemoryStream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(Encoding.ASCII.GetBytes(bytes.Length.ToString(CultureInfo.InvariantCulture) + ":"));
        stream.Write(bytes);
        stream.WriteByte(10);
        if (stream.Length > 32768) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Bounds);
    }
}
