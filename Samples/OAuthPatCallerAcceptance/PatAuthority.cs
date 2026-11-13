// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens;

namespace OAuthPatCallerAcceptance;

/// <summary>Synthetic PAT authority with atomic process-local issuance. Production requires linearizable persistence and recovery fencing.</summary>
/// <param name="store">The sample store.</param>
public sealed class PatAuthority(SampleIdentityStore store) : IPatAuthority
{
    /// <inheritdoc/>
    public Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken) => Task.FromResult(store.ReadPat(verifier));

    /// <inheritdoc/>
    public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken) => Task.FromResult(store.TryIssue(record, intent));

    /// <inheritdoc/>
    public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken) => Task.FromResult(PatRevokeResult.Refused);
}
