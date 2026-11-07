// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Explicit PAT installation limits. No persistence or policy adapter is enabled by default.</summary>
/// <param name="ResourceKey">The canonical key for this receiving resource.</param>
public sealed record PatOptions(string ResourceKey)
{
    /// <summary>Gets the issuance and verification ceiling, never greater than ninety days.</summary>
    public TimeSpan MaximumLifetime { get; init; } = TimeSpan.FromDays(90);

    /// <summary>Gets the explicit case-sensitive scope allowlist. Wildcards and administrative or approval scopes are not supported.</summary>
    public IReadOnlyCollection<string> AllowedScopes { get; init; } = [];

    /// <summary>Gets retired ASCII credential prefixes, refused under every Authorization scheme. Empty by default.</summary>
    public IReadOnlyCollection<string> ReservedLegacyPrefixes { get; init; } = [];
}
