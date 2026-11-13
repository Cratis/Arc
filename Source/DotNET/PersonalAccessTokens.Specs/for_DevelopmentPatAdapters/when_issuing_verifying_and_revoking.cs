// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters.given;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters;

public class when_issuing_verifying_and_revoking : a_development_pat_installation
{
    CredentialVerification _before;
    CredentialVerification _after;
    PatRevokeResult _revoked;
    PatRevokeResult _again;
    string? _refusal;

    async Task Because()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var issued = await _facts.Module().Issue(TimeSpan.FromHours(1), ["read"], cancellation);
        _refusal = issued.RefusalCode;
        using var token = issued.Token!;
        var raw = token.Reveal()!;
        _before = await _facts.VerifyRaw(raw, cancellation);
        var caller = new VerifiedCallerBuilder(new(new Dictionary<string, string> { [_facts.Record.Resource] = _facts.Record.Audience }))
            .Build(_before.Facts!, new(_facts.Record.Resource, _facts.Record.Environment), _facts.Record.Tenant).Caller!;
        var context = new for_PersonalAccessTokens.given.FakeIssuanceContext(() => caller);
        var module = new PersonalAccessTokens(_facts.Settings, _authority, _epochs, _policy, context, _clock, []);
        _revoked = await module.Revoke(token.CredentialId, cancellation);
        _after = await _facts.VerifyRaw(raw, cancellation);
        _again = await module.Revoke(token.CredentialId, cancellation);
    }

    [Fact] void should_issue_in_development() => _refusal.ShouldBeNull();
    [Fact] void should_verify_through_the_real_pat_source() => _before.Refusal.ShouldBeNull();
    [Fact] void should_revoke_through_the_public_module() => _revoked.ShouldEqual(PatRevokeResult.Revoked);
    [Fact] void should_refuse_the_revoked_pat_immediately() => _after.Refusal.ShouldNotBeNull();
    [Fact] void should_revoke_idempotently() => _again.ShouldEqual(PatRevokeResult.AlreadyRevoked);
}
