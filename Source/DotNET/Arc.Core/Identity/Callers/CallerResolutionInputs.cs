// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.ForwardedIdentityV2;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Digest of the request inputs consumed by caller resolution. Never retains credentials.</summary>
internal sealed class CallerResolutionInputs
{
    readonly byte[] _digest;

    CallerResolutionInputs(byte[] digest) => _digest = digest;

    /// <summary>Captures the exact consumed request inputs.</summary>
    /// <param name="request">The current request.</param>
    /// <param name="sources">The installed credential sources.</param>
    /// <param name="tenant">The resolved tenant.</param>
    /// <returns>A digest with unambiguous value framing.</returns>
    internal static CallerResolutionInputs Capture(IHttpRequestContext request, IEnumerable<IVerifiedCredentialSource> sources, TenantId? tenant)
    {
        var framed = new StringBuilder();
        if (request is not ICanReadForwardedIdentityRequest raw)
        {
            framed.Append(ForwardedIdentityCanonicalization.Frame("unreadable"))
                .Append(ForwardedIdentityCanonicalization.Frame(request.Method));
            return new(SHA256.HashData(Encoding.UTF8.GetBytes(framed.ToString())));
        }
        framed.Append(ForwardedIdentityCanonicalization.Frame(request.Method))
            .Append(ForwardedIdentityCanonicalization.Frame(raw.RawTarget ?? string.Empty));
        AppendHeaders(framed, raw, sources);
        framed.Append(ForwardedIdentityCanonicalization.Frame(tenant is null ? "none" : tenant == TenantId.NotSet ? "notset" : tenant.Value));

        // When #3704 lands: frame ForwardedIdentityHop.IsAdmitted(request).
        return new(SHA256.HashData(Encoding.UTF8.GetBytes(framed.ToString())));
    }

    /// <summary>Compares digests without exposing the consumed inputs.</summary>
    /// <param name="other">The other captured inputs.</param>
    /// <returns>Whether the inputs match.</returns>
    internal bool Matches(CallerResolutionInputs other) => CryptographicOperations.FixedTimeEquals(_digest, other._digest);

    static void AppendHeaders(StringBuilder framed, ICanReadForwardedIdentityRequest raw, IEnumerable<IVerifiedCredentialSource> sources)
    {
        foreach (var group in raw.HeaderNames.GroupBy(name => name.ToLowerInvariant()).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var name = group.Key;
            if (name is not ("authorization" or ForwardedIdentityV2Envelope.Header) && !sources.Any(source => source.Consumes(name))) continue;
            var values = raw.GetHeaderValues(group.First());
            framed.Append(ForwardedIdentityCanonicalization.Frame(name))
                .Append(ForwardedIdentityCanonicalization.Frame(values.Count.ToString(CultureInfo.InvariantCulture)));
            foreach (var value in values) framed.Append(ForwardedIdentityCanonicalization.Frame(value));
        }
    }
}
