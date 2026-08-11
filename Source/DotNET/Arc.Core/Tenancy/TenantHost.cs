// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Arc.Http;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Reduces host names to the canonical form subdomain tenancy matches base domains and tenant labels against.
/// </summary>
/// <remarks>
/// The same normalization is applied to the request host and to the configured base domain, so one domain is always
/// one tenant no matter how the request wrote it, and a base domain that can never identify a tenant is rejected
/// before the application starts serving requests.
/// </remarks>
internal static partial class TenantHost
{
    static readonly IdnMapping _idnMapping = new();

    /// <summary>
    /// Reduces a host to the canonical form the base domain is matched against, or to an empty string for a host that
    /// can never carry a tenant.
    /// </summary>
    /// <param name="value">The host to normalize.</param>
    /// <returns>The normalized host, or an empty string when the host cannot carry a tenant.</returns>
    /// <remarks>
    /// The <see cref="IPAddress"/> rejection is load bearing on the base domain side: an address literal is a sequence
    /// of label-shaped parts, so without it <c>192.168.1.10</c> would pass as a domain name and turn every
    /// <c>anything.192.168.1.10</c> host into a tenant. Trailing dots are removed after the punycode conversion
    /// because <see cref="IdnMapping.GetAscii(string)"/> maps U+3002, U+FF0E and U+FF61 to a label separator, so a
    /// host that carries no ASCII dot at all can still end in one.
    /// </remarks>
    internal static string Normalize(string value)
    {
        var host = HostName.WithoutPort(value.Trim());
        if (host.Length == 0)
        {
            return string.Empty;
        }

        if (IPAddress.TryParse(host, out _))
        {
            return string.Empty;
        }

        var lowercased = host.Trim('.').ToLowerInvariant();
        return ToAscii(lowercased).Trim('.');
    }

    /// <summary>
    /// Checks whether a value is a single DNS label that can be used as a tenant ID.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True when the value is a valid letter-digit-hyphen label, false otherwise.</returns>
    /// <remarks>
    /// The resolved tenant ID flows into the Chronicle namespace and the database name, so it is held to the letter,
    /// digit and hyphen rule for DNS labels rather than to whatever the host happened to contain.
    /// </remarks>
    internal static bool IsLabel(string value) => LabelExpression().IsMatch(value);

    /// <summary>
    /// Checks whether a value can be used as the base domain tenants are resolved in front of.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True when the value is a domain name of at least two labels, false otherwise.</returns>
    internal static bool IsDomainName(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            return false;
        }

        var labels = normalized.Split('.');
        return labels.Length > 1 && labels.All(IsLabel);
    }

    /// <summary>
    /// Converts an internationalized host to its punycode form so the same domain always yields the same tenant ID.
    /// </summary>
    /// <param name="host">The host to convert.</param>
    /// <returns>The punycode form, or an empty string when the host is not a valid internationalized domain name.</returns>
    static string ToAscii(string host)
    {
        if (Ascii.IsValid(host))
        {
            return host;
        }

        try
        {
            return _idnMapping.GetAscii(host);
        }
        catch (ArgumentException)
        {
            // Not a domain name at all, so it identifies no tenant and the header fallback takes over.
            return string.Empty;
        }
    }

    [GeneratedRegex(@"\A[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\z", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LabelExpression();
}
