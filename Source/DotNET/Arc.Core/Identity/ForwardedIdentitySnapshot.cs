// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity;

/// <summary>
/// Captures immutable host-observed inputs once, before verifying or consuming signed forwarding.
/// </summary>
/// <param name="Headers">The sole observed value for each covered header and envelope.</param>
/// <param name="Method">The observed HTTP method.</param>
/// <param name="Path">The existing host path used in the request-local cache snapshot.</param>
/// <param name="RawTarget">The observed origin-form target.</param>
internal sealed record ForwardedIdentitySnapshot(IReadOnlyDictionary<string, string> Headers, string Method, string Path, string? RawTarget)
{
    /// <summary>
    /// Determines whether the observed names contain a forwarded credential.
    /// </summary>
    /// <param name="names">The host-observed header names.</param>
    /// <returns>Whether credential material is present.</returns>
    internal static bool HasCredential(IEnumerable<string> names) => names.Any(name =>
        name.Equals(ForwardedIdentityEnvelope.Header, StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("x-cratis-on-behalf", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Copies each relevant host-observed field exactly once and rejects ambiguous observed cardinality.
    /// </summary>
    /// <param name="context">The request exposing the capability.</param>
    /// <param name="options">The host options defining the covered set.</param>
    /// <returns>The immutable copy, or null when ambiguous or over the input bounds.</returns>
    internal static ForwardedIdentitySnapshot? Capture(IHttpRequestContext context, ArcOptions options)
    {
        if (context is not ICanReadForwardedIdentityRequest raw)
        {
            return null;
        }
        var names = raw.HeaderNames.Where(name => name.Equals(ForwardedIdentityEnvelope.Header, StringComparison.OrdinalIgnoreCase) ||
                ForwardedIdentityCanonicalization.IsCovered(name, options.Tenancy?.HttpHeader))
            .Select(name => name.ToLowerInvariant()).Distinct(StringComparer.Ordinal).Take(66).ToArray();
        if (names.Length > 65)
        {
            return null;
        }
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var values = raw.GetHeaderValues(name).ToArray();
            if (name.Length > 32768 || values.Length != 1 || values[0] is not { } value || value.Length > 32768)
            {
                return null;
            }
            headers.Add(name, value);
        }
        return new(new ReadOnlyDictionary<string, string>(headers), context.Method, context.Path, raw.RawTarget);
    }
}
