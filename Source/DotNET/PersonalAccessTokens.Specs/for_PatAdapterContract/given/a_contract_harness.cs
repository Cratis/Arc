// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens.Contract;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterContract.given;

public class a_contract_harness : Specification
{
    protected static PatAdapterHarness Harness(string defect = "none") => new()
    {
        Create = _ => Task.FromResult(Instance(new(new(), defect))),
        HasIssuedIntent = (instance, id, _) => Task.FromResult(AdapterFor(instance).State.Intents.Contains(id)),
        FailNextCommit = (instance, _) => Change(instance, state => state.FailCommit = true),
        SecondProcess = (instance, _) => Task.FromResult(Instance(new(AdapterFor(instance).State, defect))),
        Snapshot = (instance, _) => Change(instance, state => state.Snapshot = state.Record),
        Restore = (instance, _) => Change(instance, state => state.Record = state.Snapshot),
        AdvanceRecoveryEpoch = (instance, _) => Change(instance, state => state.Epoch += defect == "restore fence" ? 0UL : 1UL),
        RemoveRecoveryEvidence = (instance, _) => Change(instance, state => state.Epoch = null),
        BlockAuthority = (instance, _) => Change(instance, state => state.AuthorityUnavailable = true),
        BlockRecovery = (instance, _) => Change(instance, state => state.RecoveryUnavailable = true),
        BlockPolicy = (instance, _) => Change(instance, state => state.PolicyUnavailable = true),
        DisablePerson = (instance, _) => Change(instance, state => state.PersonEnabled = false),
        RemoveMembership = (instance, _) => Change(instance, state => state.Member = false),
        ReduceLifetime = (instance, _) => Change(instance, state => state.Lifetime = TimeSpan.FromMinutes(1)),
        ReduceScopes = (instance, _) => Change(instance, state => state.ReadScope = false),
        ExpireSso = (instance, _) => Change(instance, state => state.Sso = false)
    };

    static PatAdapterInstance Instance(Adapter adapter) => new(adapter, adapter, adapter);
    static Adapter AdapterFor(PatAdapterInstance instance) => (Adapter)instance.Authority;
    static Task Change(PatAdapterInstance instance, Action<State> change)
    {
        change(AdapterFor(instance).State);
        return Task.CompletedTask;
    }

    sealed class State
    {
        internal PatRecord? Record;
        internal PatRecord? Snapshot;
        internal HashSet<CredentialId> Intents = [];
        internal ulong? Epoch = 4;
        internal bool FailCommit;
        internal bool AuthorityUnavailable;
        internal bool RecoveryUnavailable;
        internal bool PolicyUnavailable;
        internal bool PersonEnabled = true;
        internal bool Member = true;
        internal bool ReadScope = true;
        internal bool Sso = true;
        internal TimeSpan Lifetime = TimeSpan.FromDays(90);
    }

    sealed class Adapter(State state, string defect) : IPatAuthority, IPatRecoveryEpoch, IPatPolicy
    {
        PatRecord? _cached;
        internal State State => state;

        public Task<bool> TryCreate(PatRecord record, PatIssuedIntent intent, CancellationToken cancellationToken)
        {
            if (state.AuthorityUnavailable && defect != "unavailable authority") return Task.FromResult(false);
            if (state.FailCommit)
            {
                state.FailCommit = false;
                if (defect == "failed issuance rollback") state.Record = record;
                return Task.FromResult(false);
            }
            if (state.Record is not null && (state.Record.CredentialId == record.CredentialId || state.Record.Verifier == record.Verifier) && defect != "duplicate credential" && defect != "duplicate verifier") return Task.FromResult(false);
            state.Record = record;
            if (defect != "atomic issuance") state.Intents.Add(intent.CredentialId);
            return Task.FromResult(true);
        }

        public Task<PatRecord?> ReadAuthoritative(string resource, string verifier, CancellationToken cancellationToken)
        {
            if (state.AuthorityUnavailable && defect != "unavailable authority") throw new InvalidOperationException("synthetic adapter failure");
            if (defect == "second-process revocation" && _cached is not null) return Task.FromResult<PatRecord?>(_cached);
            var result = state.Record;
            if (result?.Verifier != verifier) result = null;
            if (defect != "resource partition" && result?.Resource != resource) result = null;
            _cached = result;
            return Task.FromResult(result);
        }

        public Task<PatRevokeResult> TryRevoke(PatOwner owner, CredentialId credentialId, CancellationToken cancellationToken)
        {
            var record = state.Record;
            if (record is null || record.CredentialId != credentialId) return Task.FromResult(PatRevokeResult.Refused);
            var expected = new PatOwner(record.Resource, record.Environment, record.Person, record.AccountKey, record.Tenant);
            if (defect != "wrong resource owner" && owner != expected) return Task.FromResult(PatRevokeResult.Refused);
            if (record.Revoked && defect != "revocation idempotency") return Task.FromResult(PatRevokeResult.AlreadyRevoked);
            state.Record = record with { Revoked = true };
            return Task.FromResult(PatRevokeResult.Revoked);
        }

        public Task<ulong?> Read(CancellationToken cancellationToken)
        {
            if (state.RecoveryUnavailable && defect != "unavailable recovery") throw new InvalidOperationException("synthetic recovery failure");
            if (state.Epoch is null && defect == "missing recovery evidence") state.Epoch = 4;
            return Task.FromResult(state.Epoch);
        }

        public Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken)
        {
            if (state.PolicyUnavailable && defect != "unavailable policy") throw new InvalidOperationException("synthetic policy failure");
            var permitted = defect == "disabled person" || (state.PersonEnabled && state.Member && state.ReadScope && state.Sso);
            return Task.FromResult(new PatPolicyDecision(permitted, state.Lifetime, 7));
        }
    }
}
