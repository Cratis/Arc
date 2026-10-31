// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity;

/// <summary>
/// Builds the byte-exact v1 MAC input from unflattened transport fields.
/// </summary>
internal static class ForwardedIdentityCanonicalization
{
    /// <summary>
    /// Frames a string by its UTF-8 byte length.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <returns>The length-prefixed LF-terminated frame.</returns>
    internal static string Frame(string value) => $"{Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)}:{value}\n";

    /// <summary>
    /// Determines whether a header belongs to the signed set.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="tenantHeader">The configured tenancy header.</param>
    /// <returns>Whether the name is covered.</returns>
    internal static bool IsCovered(string name, string? tenantHeader) =>
        name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("x-cratis-on-behalf", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("x-cratis-token-client-id", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("x-cratis-token-scope", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("x-cratis-tenant-id", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("tenant-id", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("x-cratis-microservice", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("service-id", StringComparison.OrdinalIgnoreCase) ||
        name.Equals(tenantHeader, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the escaped path, normalizing only percent-triplet hex case.
    /// </summary>
    /// <param name="rawTarget">The raw origin-form request target.</param>
    /// <returns>The canonical path, or null if invalid.</returns>
    internal static string? Path(string? rawTarget)
    {
        if (string.IsNullOrEmpty(rawTarget) || rawTarget[0] != '/' || rawTarget.Any(character => character is <= ' ' or >= (char)127 or '#'))
        {
            return null;
        }
        var path = rawTarget.Split('?', 2)[0].ToCharArray();
        for (var index = 0; index < path.Length; index++)
        {
            if (path[index] != '%')
            {
                continue;
            }
            if (index + 2 >= path.Length || !Uri.IsHexDigit(path[index + 1]) || !Uri.IsHexDigit(path[index + 2]))
            {
                return null;
            }
            path[++index] = char.ToUpperInvariant(path[index]);
            path[++index] = char.ToUpperInvariant(path[index]);
        }
        return new(path);
    }

    /// <summary>
    /// Captures bounded canonical input and the exact consumed transport fields.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="options">The validated host configuration.</param>
    /// <param name="envelope">The parsed envelope.</param>
    /// <returns>The captured request, or null if invalid.</returns>
    internal static ForwardedIdentityRequest? Create(IHttpRequestContext context, ArcOptions options, ForwardedIdentityEnvelope envelope)
    {
        if (context is not ICanReadForwardedIdentityRequest raw ||
            string.IsNullOrEmpty(context.Method) || context.Method.Any(character => !IsToken(character) || char.IsLower(character)) ||
            Path(raw.RawTarget) is not { } path)
        {
            return null;
        }
        var names = raw.HeaderNames.Where(name => IsCovered(name, options.Tenancy.HttpHeader) &&
                !name.Equals(ForwardedIdentityEnvelope.Header, StringComparison.OrdinalIgnoreCase))
            .Select(name => name.ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(65).ToArray();
        if (names.Length > 64)
        {
            return null;
        }
        var canonical = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v1\n");
        foreach (var value in new[] { options.ForwardedIdentity.Audience, envelope.KeyId, envelope.TimestampText, envelope.Nonce, context.Method, path, names.Length.ToString(CultureInfo.InvariantCulture) })
        {
            canonical.Append(Frame(value));
        }
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var snapshot = new StringBuilder(Frame(context.Method) + Frame(context.Path) + Frame(raw.RawTarget!) + Frame(raw.GetHeaderValues(ForwardedIdentityEnvelope.Header)[0]));
        foreach (var name in names)
        {
            var values = raw.GetHeaderValues(name);
            if (name.Any(character => !IsToken(character)) || values.Count != 1 || values[0] is not { } original || original.Length > 32768)
            {
                return null;
            }
            var value = original.Trim(' ', '\t');
            if (value.Any(character => character is < ' ' or >= (char)127))
            {
                return null;
            }
            canonical.Append(Frame(name)).Append(Frame(value));
            if (canonical.Length > 32768)
            {
                return null;
            }
            snapshot.Append(Frame(name)).Append(Frame(original));
            headers.Add(name, value);
        }
        foreach (var required in new[] { MicrosoftIdentityPlatformHeaders.PrincipalHeader, MicrosoftIdentityPlatformHeaders.IdentityIdHeader, MicrosoftIdentityPlatformHeaders.IdentityNameHeader })
        {
            if (!headers.TryGetValue(required, out var value) || string.IsNullOrEmpty(value))
            {
                return null;
            }
        }
        var bytes = Encoding.UTF8.GetBytes(canonical.ToString());
        return bytes.Length > 32768 ? null : new(bytes, headers, snapshot.ToString());
    }

    static bool IsToken(char character) => character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') || "!#$%&'*+-.^_`|~".Contains(character);
}
