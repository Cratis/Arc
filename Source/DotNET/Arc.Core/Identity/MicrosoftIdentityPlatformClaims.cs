// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Claim types reserved by Arc for metadata it authors itself while reconstructing a principal from
/// the Microsoft Identity Platform headers.
/// </summary>
/// <remarks>
/// A reserved claim type is written exclusively by Arc from the forwarded <see cref="ClientPrincipal"/>.
/// Any claim of a reserved type that is already present in the serialized principal is removed, ignoring casing,
/// before Arc writes its own value, so a caller able to reach the application cannot forge this metadata.
/// Claim types outside this set are copied through untouched, including the canonical identity claims the
/// producing ingress reserves for itself under <c>urn:cratis:identity:</c>.
/// </remarks>
public static class MicrosoftIdentityPlatformClaims
{
    /// <summary>
    /// The claim type carrying the identity provider that the ingress authenticated the caller with,
    /// taken from <see cref="ClientPrincipal.IdentityProvider"/>.
    /// </summary>
    /// <remarks>
    /// The value is the exact authentication scheme metadata the ingress forwarded. It is typically derived from a
    /// provider display name and is therefore descriptive rather than stable - do not treat it as a durable provider
    /// registration key, and do not use it as an identity or membership key. The claim is absent when the forwarded
    /// principal carries no identity provider.
    /// </remarks>
    public const string IdentityProvider = "urn:cratis:arc:identity:provider";
}
