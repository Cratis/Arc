// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity.given;

/// <summary>
/// Frozen copy of the anonymous-object fingerprint before source-generated serialization.
/// </summary>
internal static class LegacyPrincipalFingerprint
{
    internal static string For(ClaimsPrincipal? principal) =>
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
