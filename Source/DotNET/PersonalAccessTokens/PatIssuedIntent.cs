// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Framework-created, secret-free issuance intent, committed atomically with its verifier record.
/// Adapters may inspect this value but cannot create or change it.
/// </summary>
public sealed record PatIssuedIntent
{
    /// <summary>Initializes a new instance of the <see cref="PatIssuedIntent"/> class.</summary>
    /// <param name="credentialId">The checked credential identifier.</param>
    /// <param name="createdAt">The issuance timestamp.</param>
    internal PatIssuedIntent(CredentialId credentialId, DateTimeOffset createdAt)
    {
        CredentialId = credentialId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the non-secret credential identifier.
    /// </summary>
    public CredentialId CredentialId { get; internal init; }

    /// <summary>
    /// Gets the issuance timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; internal init; }

    /// <inheritdoc/>
    public override string ToString() => $"PatIssuedIntent {{ CredentialId = {CredentialId} }}";
}
