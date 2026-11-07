// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Fail-closed until an internal authoritative adapter is explicitly installed.</summary>
internal sealed class UnavailablePatAuthority : IPatAuthority
{
    /// <inheritdoc/>
    public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken) => Task.FromResult<PatRecord?>(null);

    /// <inheritdoc/>
    public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken) => Task.FromResult(PatRevokeResult.Unavailable);
}
