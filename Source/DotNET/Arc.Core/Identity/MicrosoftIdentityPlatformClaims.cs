// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Claim types reserved by Arc for metadata it authors itself while reconstructing a principal from
/// the Microsoft Identity Platform headers.
/// </summary>
/// <remarks>
/// <para>
/// A reserved claim type is written exclusively by Arc from the forwarded <see cref="ClientPrincipal"/>. Any claim
/// of a reserved type already present in the serialized principal is removed, ignoring casing, before Arc writes
/// its own value. What that buys is single provenance, not authenticity: the claim carries exactly one value and
/// that value always comes from the same field of the forwarded principal, so a claim of the reserved type passed
/// through by the ingress or by the identity provider can never displace it.
/// </para>
/// <para>
/// It does not make the value trustworthy. The <c>x-ms-client-principal</c> header is base64, not a signature, and
/// Arc does not check who sent it, so any caller that can reach the application can author the entire serialized
/// principal - including the field Arc reads. Trust this claim exactly as far as you trust that header: only
/// insofar as your ingress is the only thing that can set it. Arc's own contribution is that there is one value
/// with one provenance to reason about instead of several with unknown ones.
/// </para>
/// <para>
/// Read reserved claims with <c>ClaimsPrincipal.FindFirst</c> or <c>ClaimsPrincipal.FindAll</c>, and never
/// normalize the claim type yourself. Those lookups compare the way the removal above does, so what they return is
/// exactly what Arc wrote. Enumerating the claims and folding types with <c>ToUpperInvariant</c> or <c>Trim</c>
/// widens the match beyond what was removed - a forged type that differs only by Unicode case folding or trailing
/// whitespace then matches, and it precedes Arc's claim in the list.
/// </para>
/// <para>
/// Claim types outside this set are copied through untouched, including the canonical identity claims the
/// producing ingress reserves for itself under <c>urn:cratis:identity:</c>.
/// </para>
/// </remarks>
public static class MicrosoftIdentityPlatformClaims
{
    /// <summary>
    /// The claim type carrying the identity provider that the ingress authenticated the caller with,
    /// taken from <see cref="ClientPrincipal.IdentityProvider"/>.
    /// </summary>
    /// <remarks>
    /// The value is the exact <c>identityProvider</c> field the ingress forwarded, verbatim and untrimmed - Arc
    /// neither interprets nor normalizes it. What it means is therefore the ingress's choice rather than Arc's:
    /// Cratis AuthProxy forwards the canonical provider key, the same value it publishes as its own
    /// <c>urn:cratis:identity:provider-key</c> claim, while another ingress may forward an authentication scheme
    /// name or a provider display name that changes when the provider is renamed. Arc guarantees neither, so treat
    /// this claim as metadata for telling federations apart, for diagnostics, and for provider-aware behavior. A
    /// consumer that needs a durable provider key should read the claim the ingress publishes for that purpose -
    /// <c>urn:cratis:identity:provider-key</c> in an AuthProxy deployment - instead of this one. The claim is
    /// absent when the forwarded principal carries no identity provider, or only blank characters.
    /// </remarks>
    public const string IdentityProvider = "urn:cratis:arc:identity:provider";
}
