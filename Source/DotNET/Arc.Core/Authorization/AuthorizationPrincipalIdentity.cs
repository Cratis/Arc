// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Conservatively compares a complete standard principal, not just its subject or tenant, across nested commands.
/// </summary>
internal static class AuthorizationPrincipalIdentity
{
    /// <summary>
    /// Captures immutable principal content before application code can mutate its identities or claims.
    /// </summary>
    /// <param name="principal">The current principal.</param>
    /// <returns>An immutable identity description.</returns>
    internal static PrincipalSnapshot Capture(ClaimsPrincipal? principal) => new(
        principal,
        IsStandard(principal),
        Fingerprint(principal),
        principal?.Identities.SelectMany(identity => Walk(identity).Skip(1)).ToArray() ?? [],
        principal?.Identities.SelectMany(Walk).Select(identity => identity.BootstrapContext).ToArray() ?? []);

    /// <summary>
    /// Compares a captured identity to one selected later.
    /// </summary>
    /// <param name="snapshot">The previously captured identity.</param>
    /// <param name="principal">The proposed principal.</param>
    /// <returns>Whether both describe exactly the same standard principal.</returns>
    internal static bool Same(PrincipalSnapshot snapshot, ClaimsPrincipal? principal)
    {
        if ((!snapshot.IsStandard || !IsStandard(principal)) && !ReferenceEquals(snapshot.Reference, principal))
        {
            return false;
        }

        if (!string.Equals(snapshot.Fingerprint, Fingerprint(principal), StringComparison.Ordinal))
        {
            return false;
        }

        var identities = principal?.Identities.SelectMany(Walk).ToArray() ?? [];
        if (snapshot.BootstrapContexts.Length != identities.Length)
        {
            return false;
        }

        for (var index = 0; index < identities.Length; index++)
        {
            var context = snapshot.BootstrapContexts[index];
            if ((ReferenceEquals(snapshot.Reference, principal) || context is not null and not string) &&
                !ReferenceEquals(context, identities[index].BootstrapContext))
            {
                return false;
            }
        }

        return !ReferenceEquals(snapshot.Reference, principal) ||
            snapshot.Actors.SequenceEqual(principal?.Identities.SelectMany(identity => Walk(identity).Skip(1)) ?? [], ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// Compares two principals by immutable identity content at this moment.
    /// </summary>
    /// <param name="first">The original principal.</param>
    /// <param name="second">The selected principal.</param>
    /// <returns>Whether they are the same effective identity.</returns>
    internal static bool Same(ClaimsPrincipal? first, ClaimsPrincipal? second) => Same(Capture(first), second);

    static bool IsStandard(ClaimsPrincipal? principal) => principal is null ||
        (principal.GetType() == typeof(ClaimsPrincipal) &&
         principal.Identities.SelectMany(Walk).All(identity => identity.GetType() == typeof(ClaimsIdentity)));

    static IEnumerable<ClaimsIdentity> Walk(ClaimsIdentity identity)
    {
        yield return identity;
        if (identity.Actor is not null)
        {
            foreach (var actor in Walk(identity.Actor))
            {
                yield return actor;
            }
        }
    }

    static string Fingerprint(ClaimsPrincipal? principal) =>
        JsonSerializer.Serialize(principal?.Identities.Select(IdentityContent));

    static object IdentityContent(ClaimsIdentity identity) => new
    {
        IdentityType = identity.GetType().FullName,
        identity.AuthenticationType,
        identity.NameClaimType,
        identity.RoleClaimType,
        identity.Label,
        BootstrapContext = new
        {
            IsOpaque = identity.BootstrapContext is not null and not string,
            Value = identity.BootstrapContext as string
        },
        Actor = identity.Actor is null ? null : IdentityContent(identity.Actor),
        Claims = identity.Claims.Select(claim => new
        {
            claim.Type,
            claim.Value,
            claim.ValueType,
            claim.Issuer,
            claim.OriginalIssuer,
            Properties = claim.Properties.OrderBy(property => property.Key, StringComparer.Ordinal)
                .Select(property => new { property.Key, property.Value })
        })
    };
}
