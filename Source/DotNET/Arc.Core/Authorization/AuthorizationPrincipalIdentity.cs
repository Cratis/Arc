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
    internal static PrincipalSnapshot Capture(ClaimsPrincipal? principal) =>
        new(principal, IsStandard(principal), Fingerprint(principal));

    /// <summary>
    /// Compares a captured identity to one selected later.
    /// </summary>
    /// <param name="snapshot">The previously captured identity.</param>
    /// <param name="principal">The proposed principal.</param>
    /// <returns>Whether both describe exactly the same standard principal.</returns>
    internal static bool Same(PrincipalSnapshot snapshot, ClaimsPrincipal? principal) =>
        snapshot.IsStandard && IsStandard(principal)
            ? string.Equals(snapshot.Fingerprint, Fingerprint(principal), StringComparison.Ordinal)
            : ReferenceEquals(snapshot.Reference, principal) &&
                string.Equals(snapshot.Fingerprint, Fingerprint(principal), StringComparison.Ordinal);

    /// <summary>
    /// Compares two principals by immutable identity content at this moment.
    /// </summary>
    /// <param name="first">The original principal.</param>
    /// <param name="second">The selected principal.</param>
    /// <returns>Whether they are the same effective identity.</returns>
    internal static bool Same(ClaimsPrincipal? first, ClaimsPrincipal? second) => Same(Capture(first), second);

    static bool IsStandard(ClaimsPrincipal? principal) => principal is null ||
        (principal.GetType() == typeof(ClaimsPrincipal) && principal.Identities.All(identity => identity.GetType() == typeof(ClaimsIdentity)));

    static string Fingerprint(ClaimsPrincipal? principal) => JsonSerializer.Serialize(principal?.Identities.Select(identity => new
    {
        IdentityType = identity.GetType().FullName,
        identity.AuthenticationType,
        identity.NameClaimType,
        identity.RoleClaimType,
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
    }));
}
