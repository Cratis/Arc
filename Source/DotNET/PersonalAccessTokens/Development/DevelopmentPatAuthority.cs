// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.Development;

/// <summary>
/// Development-only, process-local PAT authority. All records and issuance intents are lost on restart.
/// This is not durable persistence, a production adapter, or a recovery/restore implementation.
/// Register with <see cref="DevelopmentPatArcBuilderExtensions.UseDevelopmentPersonalAccessTokenAdapters"/>.
/// </summary>
public sealed class DevelopmentPatAuthority : IPatAuthority
{
    readonly IHostEnvironment _environment;
    readonly TimeProvider _clock;
    readonly object _gate = new();
    readonly Dictionary<CredentialId, (PatRecord Record, PatIssuedIntent Intent)> _records = [];
    readonly Dictionary<(string Resource, string Verifier), CredentialId> _verifiers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentPatAuthority"/> class.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    /// <param name="clock">The clock.</param>
    internal DevelopmentPatAuthority(IHostEnvironment environment, TimeProvider clock)
    {
        _environment = environment;
        _clock = clock;
    }

    /// <inheritdoc/>
    public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_environment.IsDevelopment() && Create(record, intent));
    }

    /// <inheritdoc/>
    public Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_environment.IsDevelopment() ? Read(resource, verifier) : null);
    }

    /// <inheritdoc/>
    public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_environment.IsDevelopment() ? Revoke(owner, credentialId) : PatRevokeResult.Unavailable);
    }

    static bool OwnedBy(PatRecord record, PatOwner owner) =>
        record.Resource == owner.Resource && record.Environment == owner.Environment &&
        record.Person == owner.Person && record.AccountKey == owner.AccountKey && record.Tenant == owner.Tenant;

    bool Create(PatRecord record, PatIssuedIntent intent)
    {
        lock (_gate)
        {
            if (!CanCreate(record, intent)) return false;
            var snapshot = record with { Scopes = record.Scopes.ToFrozenSet(StringComparer.Ordinal) };
            _records.Add(snapshot.CredentialId, (snapshot, intent));
            _verifiers.Add((snapshot.Resource, snapshot.Verifier), snapshot.CredentialId);
            return true;
        }
    }

    bool CanCreate(PatRecord record, PatIssuedIntent intent) =>
        intent.CredentialId == record.CredentialId && intent.CreatedAt == record.CreatedAt &&
        !_records.ContainsKey(record.CredentialId) && !_verifiers.ContainsKey((record.Resource, record.Verifier));

    PatRecord? Read(string resource, string verifier)
    {
        lock (_gate)
        {
            if (!_verifiers.TryGetValue((resource, verifier), out var identifier)) return null;
            var record = _records[identifier].Record;
            return record.Revoked || record.ExpiresAt <= _clock.GetUtcNow() ? null : record;
        }
    }

    PatRevokeResult Revoke(PatOwner owner, CredentialId identifier)
    {
        lock (_gate)
        {
            if (!_records.TryGetValue(identifier, out var entry)) return PatRevokeResult.Refused;
            if (!OwnedBy(entry.Record, owner)) return PatRevokeResult.Refused;
            if (entry.Record.Revoked) return PatRevokeResult.AlreadyRevoked;
            _records[identifier] = (entry.Record with { Revoked = true }, entry.Intent);
            return PatRevokeResult.Revoked;
        }
    }
}
