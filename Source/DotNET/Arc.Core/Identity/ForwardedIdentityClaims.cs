// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity;

/// <summary>
/// Builds the claims of a forwarded <see cref="ClientPrincipal"/>, with identity claims taken from the forward itself.
/// </summary>
internal static class ForwardedIdentityClaims
{
    /// <summary>
    /// Builds the claims for a forwarded principal.
    /// </summary>
    /// <param name="principal">The forwarded <see cref="ClientPrincipal"/>.</param>
    /// <param name="identityId">The identity id the forward asserts for the principal.</param>
    /// <returns>The claims of the principal.</returns>
    /// <remarks>
    /// Serialized subject and identity provider claims are replaced, so an identity provider cannot supply a claim
    /// that collides with the values the ingress asserts.
    /// </remarks>
    internal static List<Claim> From(ClientPrincipal principal, string identityId)
    {
        var claims = principal.Claims.Select(claim => new Claim(claim.typ, claim.val)).ToList();

        claims.RemoveAll(claim =>
            claim.Type == ClaimTypes.NameIdentifier ||
            claim.Type == "sub" ||
            claim.Type.Equals(MicrosoftIdentityPlatformClaims.IdentityProvider, StringComparison.OrdinalIgnoreCase));
        claims.Add(new Claim(ClaimTypes.Name, principal.UserDetails));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, identityId));
        claims.Add(new Claim("sub", identityId));

        if (!string.IsNullOrWhiteSpace(principal.IdentityProvider))
        {
            claims.Add(new Claim(MicrosoftIdentityPlatformClaims.IdentityProvider, principal.IdentityProvider));
        }

        claims.AddRange(principal.UserRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}
