// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Issues and revokes PATs for the current verified person. No raw credential is persisted.</summary>
public interface IPersonalAccessTokens
{
    /// <summary>Issues only after current policy checks and an acknowledged atomic verifier/intent commit.</summary>
    /// <param name="lifetime">The requested positive lifetime, at most ninety days.</param>
    /// <param name="scopes">Explicit requested scope ceilings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A single-use reveal handle, or a secret-free refusal.</returns>
    Task<PatIssueResult> Issue(TimeSpan lifetime, IEnumerable<string> scopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes only a PAT credential owned by the current verified person and tenant, authenticated with a PAT credential.
    /// Other caller credential kinds are refused.
    /// </summary>
    /// <param name="credentialId">The non-secret credential identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The idempotent revocation outcome.</returns>
    Task<PatRevokeResult> Revoke(CredentialId credentialId, CancellationToken cancellationToken = default);
}
