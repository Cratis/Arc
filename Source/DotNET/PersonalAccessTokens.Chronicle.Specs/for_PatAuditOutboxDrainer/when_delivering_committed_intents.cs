// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer;

public class when_delivering_committed_intents : given.an_audit_outbox
{
    async Task Because() => _result = await _drainer.Drain(TestContext.Current.CancellationToken);

    [Fact] void should_succeed() => _result.ShouldBeTrue();
    [Fact] void should_acknowledge_both_intents() => _acknowledged.ShouldEqual(new[] { _issuance.Id, _revocation.Id });
    [Fact] async Task should_append_both_facts() => (await Events()).Length.ShouldEqual(2);

    [Fact]
    async Task should_fold_owner_listing_and_audit_timestamps()
    {
        using var scenario = new ReadModelScenario<PatAuditEntry>().WithProjection(new PatAuditProjection().Define);
        await scenario.ProcessAppendedEvents(await Events());
        var entry = scenario.Instance!;
        entry.CredentialId.ShouldEqual(_issuance.Metadata.CredentialId);
        entry.Resource.ShouldEqual(_issuance.Metadata.Resource);
        entry.Environment.ShouldEqual(_issuance.Metadata.Environment);
        entry.Person.ShouldEqual(_issuance.Metadata.Person);
        entry.Tenant.ShouldEqual(_issuance.Metadata.Tenant);
        entry.Scopes.ShouldEqual(_issuance.Metadata.Scopes);
        entry.ExpiresAt.ShouldEqual(_issuance.Metadata.ExpiresAt);
        entry.Assurance.Idp.ShouldEqual(_issuance.Metadata.Assurance.Idp);
        entry.Assurance.AuthTime.ShouldEqual(_issuance.Metadata.Assurance.AuthTime);
        entry.Assurance.Acr.ShouldEqual(_issuance.Metadata.Assurance.Acr);
        entry.Assurance.Amr!.ShouldEqual(_issuance.Metadata.Assurance.Amr!);
        entry.RestoreEpoch.ShouldEqual(4UL);
        entry.IssuedAt.ShouldEqual(IssuedAt);
        entry.RevokedAt.ShouldEqual(_revocation.OccurredAt);
    }

    [Fact]
    async Task should_never_serialize_token_secret_or_full_verifier()
    {
        using var scenario = new ReadModelScenario<PatAuditEntry>().WithProjection(new PatAuditProjection().Define);
        var events = await Events();
        await scenario.ProcessAppendedEvents(events);
        var serialized = JsonSerializer.Serialize(events) + JsonSerializer.Serialize(scenario.Instance) + JsonSerializer.Serialize(_issuance);
        foreach (var forbidden in new[] { ForbiddenToken, ForbiddenSecret, ForbiddenVerifier, "\"Token\"", "\"Secret\"", "\"Verifier\"", "\"AccountKey\"" })
        {
            serialized.Contains(forbidden, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
        scenario.Instance!.VerifierId.ShouldEqual(_issuance.Metadata.VerifierId);
        scenario.Instance.VerifierId.Length.ShouldEqual(64);
    }
}
