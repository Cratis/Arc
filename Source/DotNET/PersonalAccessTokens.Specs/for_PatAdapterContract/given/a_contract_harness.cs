// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens.Contract;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterContract.given;

public class a_contract_harness : Specification
{
    protected static PatAdapterHarness Harness(string defect = "none") => new() { CreateInstance = () => Create(defect) };

    static PatAdapterInstance Create(string defect)
    {
        var adapter = new ReferenceAdapter(defect);
        return new(adapter, adapter, adapter)
        {
            AdvanceRecoveryEpoch = () =>
            {
                adapter.Epoch++;
                return Task.CompletedTask;
            },
            Restore = () =>
            {
                if (defect != "restore fence") adapter.Epoch++;
                return Task.CompletedTask;
            },
            RemoveRecoveryEvidence = () =>
            {
                adapter.Epoch = null;
                return Task.CompletedTask;
            },
            SetOutage = outage =>
            {
                adapter.Outage = outage;
                return Task.CompletedTask;
            },
            DenyEverything = () =>
            {
                adapter.Permitted = false;
                return Task.CompletedTask;
            },
            Permit = maximum =>
            {
                adapter.Maximum = maximum;
                adapter.Permitted = true;
                return Task.CompletedTask;
            }
        };
    }

    sealed class ReferenceAdapter(string defect) : IPatAuthority, IPatRecoveryEpoch, IPatPolicy
    {
        readonly object _gate = new();
        readonly Dictionary<string, PatRecord> _records = [];
        readonly Dictionary<string, PatRecord> _cache = [];
        internal ulong? Epoch = 4;
        internal bool Outage;
        internal bool Permitted = true;
        internal TimeSpan Maximum = TimeSpan.FromDays(90);

        public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken)
        {
            if (defect != "cancelled create") cancellationToken.ThrowIfCancellationRequested();
            if (Outage && defect != "authority outage") return Task.FromResult(false);
            lock (_gate)
            {
                if (defect != "duplicate verifier" && _records.ContainsKey(record.Verifier)) return Task.FromResult(false);
                if (defect != "duplicate credential" && _records.Values.Any(stored => stored.CredentialId == record.CredentialId)) return Task.FromResult(false);
                _records[record.Verifier] = Copy(record);
                return Task.FromResult(true);
            }
        }

        public Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Outage && defect != "authority outage") throw new InvalidOperationException("synthetic adapter failure");
            lock (_gate)
            {
                if (defect == "revoke-before-read ordering" && _cache.TryGetValue(verifier, out var cached)) return Task.FromResult<PatRecord?>(cached);
                if (!_records.TryGetValue(verifier, out var record)) return Task.FromResult<PatRecord?>(null);
                if (defect != "authoritative lookup" && record.Resource != resource) return Task.FromResult<PatRecord?>(null);
                _cache[verifier] = record;
                return Task.FromResult<PatRecord?>(Copy(record));
            }
        }

        public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Outage) return Task.FromResult(PatRevokeResult.Unavailable);
            lock (_gate)
            {
                var record = _records.Values.SingleOrDefault(record => record.CredentialId == credentialId);
                if (record is null) return Task.FromResult(PatRevokeResult.Refused);
                var expected = new PatOwner(record.Resource, record.Environment, record.Person, record.AccountKey, record.Tenant);
                if (defect != "owner-bound revocation" && owner != expected) return Task.FromResult(PatRevokeResult.Refused);
                if (record.Revoked) return Task.FromResult(PatRevokeResult.AlreadyRevoked);
                if (defect == "revoke-before-read ordering") _cache[record.Verifier] = record;
                _records[record.Verifier] = record with { Revoked = true };
                return Task.FromResult(PatRevokeResult.Revoked);
            }
        }

        public Task<ulong?> Read(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Outage) throw new InvalidOperationException("synthetic recovery failure");
            if (Epoch is null && defect == "missing recovery evidence") Epoch = 0;
            return Task.FromResult(Epoch);
        }

        public Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Outage && defect != "policy outage") throw new InvalidOperationException("synthetic policy failure");
            var permitted = Permitted && Maximum > TimeSpan.Zero;
            if (defect == "positive policy lifetime") permitted = Permitted;
            return Task.FromResult(new PatPolicyDecision(permitted, Maximum, 7));
        }

        PatRecord Copy(PatRecord record) => defect == "immutable stored metadata" ? record : record with
        {
            Scopes = record.Scopes.ToHashSet(StringComparer.Ordinal),
            Assurance = record.Assurance with { Amr = record.Assurance.Amr?.ToArray() }
        };
    }
}
