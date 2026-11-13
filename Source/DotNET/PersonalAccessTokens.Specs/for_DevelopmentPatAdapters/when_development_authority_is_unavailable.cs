// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.Development;
using Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters.given;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters;

public class when_development_authority_is_unavailable : a_development_pat_installation
{
    PatRecord? _expired;
    PatRecord? _restarted;
    PatRecord? _outsideDevelopment;
    bool _created;
    ulong? _epoch;
    PatPolicyDecision _policyDecision;
    PatRevokeResult _revocation;

    async Task Because()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await _facts.Create(cancellation);
        _clock.Offset = TimeSpan.FromHours(2);
        _expired = await _facts.Read(cancellation);
        _clock.Offset = TimeSpan.Zero;
        var restarted = new DevelopmentPatAuthority(_environment, _clock);
        _restarted = await restarted.ReadAuthoritative(_facts.Record.Resource, _facts.Record.Verifier, cancellation);
        _environment.EnvironmentName.Returns(Environments.Production);
        _outsideDevelopment = await _facts.Read(cancellation);
        _created = await _facts.Create(cancellation);
        _epoch = await _epochs.Read(cancellation);
        _policyDecision = await _policy.Evaluate(_facts.Record, false, cancellation);
        _revocation = await _authority.TryRevoke(_facts.Owner, _facts.Record.CredentialId, cancellation);
    }

    [Fact] void should_not_serve_expired_records() => _expired.ShouldBeNull();
    [Fact] void should_lose_every_record_on_restart() => _restarted.ShouldBeNull();
    [Fact] void should_refuse_reads_outside_development() => _outsideDevelopment.ShouldBeNull();
    [Fact] void should_refuse_creation_outside_development() => _created.ShouldBeFalse();
    [Fact] void should_withhold_recovery_evidence_outside_development() => _epoch.ShouldBeNull();
    [Fact] void should_deny_policy_outside_development() => _policyDecision.Permitted.ShouldBeFalse();
    [Fact] void should_refuse_revocation_outside_development() => _revocation.ShouldEqual(PatRevokeResult.Unavailable);
}
