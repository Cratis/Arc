// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.ForwardedIdentityV2;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        framed.Append(ForwardedIdentityCanonicalization.Frame(request.Method));
        AppendTarget(framed, raw.RawTarget);
        AppendHeaders(framed, raw, sources);
        AppendTenant(framed, tenant);

        AppendHop(framed, request);
        return new(SHA256.HashData(Encoding.UTF8.GetBytes(framed.ToString())));
    }

    /// <summary>Compares digests without exposing the consumed inputs.</summary>
    /// <param name="other">The other captured inputs.</param>
    /// <returns>Whether the inputs match.</returns>
    internal bool Matches(CallerResolutionInputs other) => CryptographicOperations.FixedTimeEquals(_digest, other._digest);

    static void AppendTarget(StringBuilder framed, string? target)
    {
        framed.Append(ForwardedIdentityCanonicalization.Frame(target is null ? "target:none" : "target:value"));
        if (target is not null) framed.Append(ForwardedIdentityCanonicalization.Frame(target));
    }

    static void AppendTenant(StringBuilder framed, TenantId? tenant)
    {
        if (tenant is null)
        {
            framed.Append(ForwardedIdentityCanonicalization.Frame("tenant:none"));
            return;
        }
        if (tenant == TenantId.NotSet)
        {
            framed.Append(ForwardedIdentityCanonicalization.Frame("tenant:notset"));
            return;
        }
        framed.Append(ForwardedIdentityCanonicalization.Frame("tenant:value"))
            .Append(ForwardedIdentityCanonicalization.Frame(tenant.Value));
    }

    static void AppendHop(StringBuilder framed, IHttpRequestContext request)
    {
        var admission = request.RequestServices?.GetService<IOptions<ArcOptions>>() is { } options
            ? ForwardedIdentityHop.Admit(request, options.Value)
            : ForwardedIdentityHopAdmission.NotRequired;
        framed.Append(ForwardedIdentityCanonicalization.Frame("hop:admission"))
            .Append(ForwardedIdentityCanonicalization.Frame(((int)admission).ToString(CultureInfo.InvariantCulture)));
        if (request is not ICanReadClientCertificate transport)
        {
            framed.Append(ForwardedIdentityCanonicalization.Frame("hop:no-transport"));
            return;
        }
        using var certificate = transport.TransportClientCertificate;
        framed.Append(ForwardedIdentityCanonicalization.Frame(certificate is null ? "hop:no-certificate" : "hop:certificate"));
        if (certificate is not null) framed.Append(ForwardedIdentityCanonicalization.Frame(certificate.GetCertHashString(HashAlgorithmName.SHA256)));
    }

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
