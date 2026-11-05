// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>An immutable raw request snapshot; never log its transport values.</summary>
internal sealed record ForwardedIdentityV2Request
{
    /// <summary>Copies raw fields before any normalization.</summary>
    /// <param name="method">The raw method token.</param>
    /// <param name="rawTarget">The raw origin-form target.</param>
    /// <param name="headers">The raw header occurrences.</param>
    /// <param name="replaySensitive">Whether to require atomic nonce reservation.</param>
    internal ForwardedIdentityV2Request(string method, string rawTarget, IReadOnlyList<ForwardedIdentityV2Header> headers, bool replaySensitive)
    {
        Method = method;
        RawTarget = rawTarget;
        Headers = Array.AsReadOnly(headers.ToArray());
        ReplaySensitive = replaySensitive;
    }

    /// <summary>Gets the raw method.</summary>
    internal string Method { get; }

    /// <summary>Gets the raw target.</summary>
    internal string RawTarget { get; }

    /// <summary>Gets the copied header occurrences.</summary>
    internal IReadOnlyList<ForwardedIdentityV2Header> Headers { get; }

    /// <summary>Gets whether this request requires replay protection.</summary>
    internal bool ReplaySensitive { get; }

    /// <summary>Captures a bounded snapshot from a cardinality-preserving raw host.</summary>
    /// <param name="request">The current raw request.</param>
    /// <param name="settings">The explicit installation settings.</param>
    /// <returns>The immutable snapshot, or null if it cannot be read safely.</returns>
    internal static ForwardedIdentityV2Request? Capture(IHttpRequestContext request, ForwardedIdentityV2Settings settings)
    {
        if (request is not ICanReadForwardedIdentityRequest raw || raw.RawTarget is not { } target) return null;
        var names = raw.HeaderNames.Where(name => ForwardedIdentityV2Canonicalization.IsCovered(name, settings.TenantHeader) || name.Equals(ForwardedIdentityV2Envelope.Header, StringComparison.OrdinalIgnoreCase) || name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Take(67).ToArray();
        if (names.Length > 66) return null;
        var headers = new List<ForwardedIdentityV2Header>();
        foreach (var name in names)
        {
            var values = raw.GetHeaderValues(name).Take(2).ToArray();
            if (name.Length > 32768 || values.Length != 1 || values[0] is not { } value || value.Length > 32768) return null;
            headers.Add(new(name, value));
        }
        return new(request.Method, target, headers, true);
    }
}
