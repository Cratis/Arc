// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Internal persistence boundary. No adapter is supplied by the core module.</summary>
internal interface IPatAuthority
{
    /// <summary>Atomically creates a unique verifier and its secret-free issuance intent; false means nothing was committed.</summary>
    /// <param name="record">Verifier metadata, never the secret.</param>
    /// <param name="intent">Secret-free issuance intent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the atomic commit was acknowledged.</returns>
    Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken);

    /// <summary>Reads active authoritative state, bypassing positive caches and failing on unavailable or uncertain state.</summary>
    /// <param name="resource">Exact receiving resource.</param>
    /// <param name="verifier">Derived verifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current authoritative metadata, or null when absent.</returns>
    Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken);

    /// <summary>Atomically checks every owner binding and revokes. Repeated owner revocation is idempotent; another owner is refused.</summary>
    /// <param name="owner">Complete verified ownership binding.</param>
    /// <param name="credentialId">Non-secret credential identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The atomic revocation outcome.</returns>
    Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken);
}
