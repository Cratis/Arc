// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer.given;

public class an_audit_outbox : Specification
{
    internal static readonly DateTimeOffset IssuedAt = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    internal const string ForbiddenToken = "dpat_fixture_forbidden_full_token";
    internal const string ForbiddenSecret = "fixture-forbidden-secret";
    internal const string ForbiddenVerifier = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    internal IPatAuditOutbox _outbox;
    internal PatAuditIntent _issuance;
    internal PatAuditIntent _revocation;
    internal EventStoreForTesting _store;
    internal PatAuditOutboxDrainer _drainer;
    internal List<PatAuditIntent> _pending;
    internal List<Guid> _acknowledged;
    internal bool _result;

    void Establish()
    {
        var record = new PatRecord(
            new CredentialId("credential-one"),
            ForbiddenVerifier,
            "resource-one",
            "resource-one",
            "https://resource-one.example",
            "test",
            new CallerPersonId("person-one"),
            new CallerAccountKey("https://identity.example", "subject-one"),
            new TenantId("tenant-one"),
            null,
            new HashSet<string> { "read" },
            IssuedAt,
            IssuedAt.AddDays(1),
            new CallerAssurance("https://identity.example", IssuedAt.ToUnixTimeSeconds(), "assurance-one", ["sso"]),
            4UL,
            1UL);
        var metadata = PatAuditMetadata.From(record);
        _issuance = new(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), metadata, IssuedAt, false);
        _revocation = new(new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), metadata, IssuedAt.AddMinutes(5), true);
        _pending = [_issuance, _revocation];
        _acknowledged = [];
        _outbox = Substitute.For<IPatAuditOutbox>();
        _outbox.ReadPending(Arg.Any<CancellationToken>()).Returns(_ => _pending.ToArray());
        _outbox.Acknowledge(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var id = call.Arg<Guid>();
            _acknowledged.Add(id);
            _pending.RemoveAll(intent => intent.Id == id);
            return Task.CompletedTask;
        });
        _store = new(new DefaultServiceProvider());
        _drainer = new(_outbox, _store.EventLog);
    }

    internal async Task<AppendedEvent[]> Events() => (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
}
