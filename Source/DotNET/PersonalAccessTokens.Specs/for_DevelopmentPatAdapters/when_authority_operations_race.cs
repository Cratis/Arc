// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters.given;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters;

public class when_authority_operations_race : a_development_pat_installation
{
    bool[] _created;
    PatRevokeResult[] _revoked;
    bool _duplicateId;
    bool _duplicateVerifier;
    PatRecord? _snapshot;
    PatRecord? _after;
    PatRevokeResult[] _foreignOwners;

    async Task Because()
    {
        var cancellation = TestContext.Current.CancellationToken;
        _created = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => _facts.Create(cancellation), cancellation)));
        _duplicateId = await _authority.TryCreate(_facts.Record with { Verifier = "other-verifier" }, _facts.Intent, cancellation);
        var another = _facts.Record with { CredentialId = new("another-id") };
        _duplicateVerifier = await _authority.TryCreate(another, new(another.CredentialId, another.CreatedAt), cancellation);
        ((HashSet<string>)_facts.Record.Scopes).Add("later-mutation");
        _snapshot = await _facts.Read(cancellation);
        var owners = new[]
        {
            _facts.Owner with { Resource = "other-resource" },
            _facts.Owner with { Environment = "other-environment" },
            _facts.Owner with { Person = new("other-person") },
            _facts.Owner with { AccountKey = new("https://other.example.test", "other-account") },
            _facts.Owner with { Tenant = new("other-tenant") }
        };
        _foreignOwners = await Task.WhenAll(owners.Select(owner => _authority.TryRevoke(owner, _facts.Record.CredentialId, cancellation)));
        _revoked = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => _authority.TryRevoke(_facts.Owner, _facts.Record.CredentialId, cancellation), cancellation)));
        _after = await _facts.Read(cancellation);
    }

    [Fact] void should_create_exactly_once() => _created.Count(_ => _).ShouldEqual(1);
    [Fact] void should_refuse_duplicate_identifiers() => _duplicateId.ShouldBeFalse();
    [Fact] void should_refuse_duplicate_verifiers() => _duplicateVerifier.ShouldBeFalse();
    [Fact] void should_snapshot_the_scope_ceiling() => _snapshot!.Scopes.ShouldContainOnly("read");
    [Fact] void should_check_every_owner_binding() => _foreignOwners.Distinct().ShouldContainOnly(PatRevokeResult.Refused);
    [Fact] void should_revoke_exactly_once() => _revoked.Count(_ => _ == PatRevokeResult.Revoked).ShouldEqual(1);
    [Fact] void should_make_concurrent_repeats_idempotent() => _revoked.Count(_ => _ == PatRevokeResult.AlreadyRevoked).ShouldEqual(19);
    [Fact] void should_not_serve_a_revoked_record() => _after.ShouldBeNull();
}
