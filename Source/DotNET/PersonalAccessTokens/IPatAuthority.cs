// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Trusted-host persistence contract, not a sandbox. No adapter is supplied by the core module.
/// Store only one-way verifiers; deny on uncertainty and never log credential material.
/// </summary>
public interface IPatAuthority
{
    /// <summary>Atomically creates a unique verifier and its secret-free issuance intent; false means nothing was committed.</summary>
    /// <remarks>An audit-enabled adapter also commits a stable audit snapshot in this transaction. The audit outbox remains internal.</remarks>
    /// <param name="record">Verifier metadata, never the secret.</param>
    /// <param name="intent">Secret-free issuance intent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the atomic commit was acknowledged.</returns>
    Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken);

    /// <summary>
    /// Reads active authoritative state with linearizable reads ordered against revocation and recovery-epoch advancement.
    /// </summary>
    /// <remarks>
    /// Bypass positive caches. Adapters that cannot guarantee this ordering must fail closed on unavailable or uncertain state.
    /// </remarks>
    /// <param name="resource">Exact receiving resource.</param>
    /// <param name="verifier">Derived verifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current authoritative metadata, or null when absent.</returns>
    Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken);

    /// <summary>Atomically checks every owner binding and revokes. Repeated owner revocation is idempotent; another owner is refused.</summary>
    /// <remarks>An audit-enabled adapter commits one revocation snapshot in the same transaction. Repeated revocation must not enqueue another intent. Chronicle delivery is never the revocation fence.</remarks>
    /// <param name="owner">Complete verified ownership binding.</param>
    /// <param name="credentialId">Non-secret credential identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The atomic revocation outcome.</returns>
    Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken);
}
