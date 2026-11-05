// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Explicit immutable verification configuration; never inferred from forwarded fields.</summary>
internal sealed record ForwardedIdentityV2Settings
{
    /// <summary>Initializes explicit installation settings and snapshots the key map.</summary>
    /// <param name="audience">The configured audience.</param>
    /// <param name="resource">The configured resource.</param>
    /// <param name="realmIssuer">The trusted account issuer.</param>
    /// <param name="tenantHeader">The optional configured tenant field.</param>
    /// <param name="keys">The validated public keys.</param>
    /// <param name="v2Only">Whether non-v2 envelopes must be refused.</param>
    internal ForwardedIdentityV2Settings(string audience, string resource, string realmIssuer, string? tenantHeader, IReadOnlyDictionary<string, ForwardedIdentityV2PublicKey> keys, bool v2Only)
    {
        Audience = audience;
        Resource = resource;
        RealmIssuer = realmIssuer;
        TenantHeader = tenantHeader;
        Keys = keys.ToFrozenDictionary(StringComparer.Ordinal);
        V2Only = v2Only;
    }

    /// <summary>Gets the configured audience.</summary>
    internal string Audience { get; }

    /// <summary>Gets the configured resource.</summary>
    internal string Resource { get; }

    /// <summary>Gets the trusted account issuer.</summary>
    internal string RealmIssuer { get; }

    /// <summary>Gets the configured tenant field.</summary>
    internal string? TenantHeader { get; }

    /// <summary>Gets the snapshotted ordinal public-key map.</summary>
    internal IReadOnlyDictionary<string, ForwardedIdentityV2PublicKey> Keys { get; }

    /// <summary>Gets whether only v2 envelopes are supported.</summary>
    internal bool V2Only { get; }
}
