// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>
/// The header names that belong to signed forwarded identity v2.
/// </summary>
/// <remarks>
/// Applications that accept other credentials on selected endpoints use this set to refuse forwarded identity
/// on those endpoints without duplicating the names. Header names are compared case-insensitively.
/// </remarks>
public static class ForwardedIdentityV2Headers
{
    /// <summary>
    /// The name of the header carrying the v2 signature.
    /// </summary>
    public const string SignatureHeader = "x-cratis-forwarded-identity-signature";

    /// <summary>
    /// The prefix of the principal headers covered by the signature.
    /// </summary>
    public const string PrincipalHeaderPrefix = "x-ms-client-principal";

    /// <summary>
    /// The prefix of the on-behalf-of headers covered by the signature.
    /// </summary>
    public const string OnBehalfOfHeaderPrefix = "x-cratis-on-behalf";

    /// <summary>
    /// Gets the exact names of the covered headers, excluding the signature header and the prefix families.
    /// </summary>
    public static readonly FrozenSet<string> CoveredHeaders = new[]
    {
        "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id",
        "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch", "x-cratis-sender-binding", "x-cratis-forwarded-token-sha256"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether a header is the signature header or one covered by it.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="tenantHeader">The configured tenant header, if any.</param>
    /// <returns>True if the header belongs to signed forwarded identity v2; otherwise false.</returns>
    public static bool Includes(string name, string? tenantHeader = null) =>
        name.Equals(SignatureHeader, StringComparison.OrdinalIgnoreCase) || IsCovered(name, tenantHeader);

    /// <summary>
    /// Determines whether a header is covered by the signature, excluding the signature header itself.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="tenantHeader">The configured tenant header, if any.</param>
    /// <returns>True if the signature covers the header; otherwise false.</returns>
    internal static bool IsCovered(string name, string? tenantHeader) =>
        !name.Equals(SignatureHeader, StringComparison.OrdinalIgnoreCase) &&
        (name.StartsWith(PrincipalHeaderPrefix, StringComparison.OrdinalIgnoreCase) ||
         name.StartsWith(OnBehalfOfHeaderPrefix, StringComparison.OrdinalIgnoreCase) ||
         CoveredHeaders.Contains(name) ||
         name.Equals(tenantHeader, StringComparison.OrdinalIgnoreCase));
}
