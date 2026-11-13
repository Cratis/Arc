// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Evidence obtained only after independent access-token verification.</summary>
public sealed class OAuthBearerVerified
{
    readonly IReadOnlySet<string> _scopes = FrozenSet<string>.Empty;

    /// <summary>Gets the verified account issuer.</summary>
    public required string Issuer { get; init; }

    /// <summary>Gets the verified account subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the verified credential identifier.</summary>
    public required string CredentialId { get; init; }

    /// <summary>Gets scopes granted by the verified token, not effective permissions.</summary>
    public required IReadOnlySet<string> Scopes { get => _scopes; init => _scopes = value.ToFrozenSet(StringComparer.Ordinal); }

    /// <summary>Gets the verified client identifier, or null when absent.</summary>
    public required string? ClientId { get; init; }

    /// <summary>Gets the token's confirmation key thumbprint. Explicit null attests that no cnf claim is present.</summary>
    /// <remarks>Arc refuses every non-null value, including an empty string; sender-constrained OAuth callers are not supported.</remarks>
    public required string? ConfirmationKeyThumbprint { get; init; }
}
