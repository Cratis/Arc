// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>Observes verifier-only persistence and supplies hostile metadata to the framework.</summary>
/// <param name="inner">The real authority under test.</param>
/// <param name="transform">An optional hostile metadata transformation.</param>
internal sealed class PatContractAuthority(IPatAuthority inner, Func<PatRecord, PatRecord>? transform = null) : IPatAuthority
{
    /// <summary>Gets the last attempted verifier record.</summary>
    internal PatRecord? AttemptedRecord { get; private set; }

    /// <summary>Gets the last authoritative verifier lookup.</summary>
    internal string? Lookup { get; private set; }

    /// <inheritdoc/>
    public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken)
    {
        AttemptedRecord = record;
        return inner.TryCreate(record, intent, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken)
    {
        Lookup = verifier;
        var record = await inner.ReadAuthoritative(resource, verifier, cancellationToken);
        return record is not null && transform is not null ? transform(record) : record;
    }

    /// <inheritdoc/>
    public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken) => inner.TryRevoke(owner, credentialId, cancellationToken);
}
