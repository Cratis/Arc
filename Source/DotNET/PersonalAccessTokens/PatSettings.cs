// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Validated immutable installation limits.</summary>
/// <param name="ResourceKey">Canonical format key.</param>
/// <param name="Target">Exact resource binding.</param>
/// <param name="MaximumLifetime">Local lifetime ceiling.</param>
/// <param name="AllowedScopes">Explicit scope ceiling.</param>
/// <param name="ReservedLegacyPrefixes">Explicit retired namespaces.</param>
internal sealed record PatSettings(PatResourceKey ResourceKey, CallerCredentialTarget Target, TimeSpan MaximumLifetime, IReadOnlySet<string> AllowedScopes, IReadOnlyList<string> ReservedLegacyPrefixes)
{
    /// <summary>The non-overridable ninety-day ceiling.</summary>
    internal static readonly TimeSpan AbsoluteMaximumLifetime = TimeSpan.FromDays(90);

    /// <summary>Validates and snapshots caller-owned options.</summary>
    /// <param name="options">Requested options.</param>
    /// <param name="target">Receiving target.</param>
    /// <returns>Immutable validated settings.</returns>
    /// <exception cref="ArgumentException">Lifetime, scopes or retired namespaces are invalid.</exception>
    internal static PatSettings Create(PatOptions options, CallerCredentialTarget target)
    {
        var key = new PatResourceKey(options.ResourceKey);
        if (options.MaximumLifetime <= TimeSpan.Zero || options.MaximumLifetime > AbsoluteMaximumLifetime)
        {
            throw new ArgumentException("PAT lifetime must be positive and at most ninety days.", nameof(options));
        }
        var scopes = options.AllowedScopes.ToFrozenSet(StringComparer.Ordinal);
        if (!scopes.All(IsSupportedScope)) throw new ArgumentException("PAT scopes must be explicit non-administrative scopes.", nameof(options));
        var prefixes = options.ReservedLegacyPrefixes.ToArray();
        if (prefixes.Any(prefix => string.IsNullOrEmpty(prefix) || prefix.Length > 64 || !prefix.EndsWith('_') || prefix.Any(character => character is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-')) || PatNamespace.Classify(prefix) == PresentedCredentialShape.PatBearer || $"{key.Value}_pat_".StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Legacy prefixes must be explicit ASCII namespaces distinct from the PAT namespace.", nameof(options));
        }
        return new(key, target, options.MaximumLifetime, scopes, Array.AsReadOnly(prefixes));
    }

    /// <summary>Checks explicit non-privileged scope ceilings.</summary>
    /// <param name="scopes">Requested or recorded scopes.</param>
    /// <returns>Whether all scopes are supported and allowed.</returns>
    internal bool Allows(IReadOnlySet<string> scopes) => scopes.All(scope => IsSupportedScope(scope) && AllowedScopes.Contains(scope));

    static bool IsSupportedScope(string scope) => !string.IsNullOrWhiteSpace(scope) && !scope.Contains('*', StringComparison.Ordinal)
        && !scope.Split([':', '.', '/', '-']).Any(segment => segment.Equals("admin", StringComparison.OrdinalIgnoreCase) || segment.Equals("approve", StringComparison.OrdinalIgnoreCase));
}
