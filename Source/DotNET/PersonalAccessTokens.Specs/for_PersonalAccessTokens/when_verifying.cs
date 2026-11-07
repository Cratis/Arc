// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_verifying : given.a_pat_module
{
    [Fact]
    public async Task should_yield_a_real_accessor_caller_without_refreshing_auth_time()
    {
        var token = await Issue();
        _clock.GetUtcNow().Returns(Now.AddDays(2));
        var caller = (await Resolver("Bearer " + token).GetCurrent()).Caller!;
        caller.Person.ShouldEqual(_person);
        caller.Tenant.ShouldEqual(_tenant);
        caller.CredentialId.ShouldEqual(_record!.CredentialId);
        caller.CredentialKind.ShouldEqual(CallerCredentialKind.PersonalAccessToken);
        caller.Assurance!.AuthTime.ShouldEqual(_caller.Assurance!.AuthTime);
    }

    [Theory]
    [InlineData("long-lifetime")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("resource")]
    [InlineData("audience")]
    [InlineData("account")]
    [InlineData("organization")]
    [InlineData("environment")]
    [InlineData("digest")]
    [InlineData("revoked")]
    [InlineData("missing")]
    [InlineData("scope")]
    [InlineData("assurance")]
    [InlineData("old-epoch")]
    [InlineData("missing-epoch")]
    [InlineData("future-epoch")]
    public async Task should_refuse_hostile_records_without_calling_policy(string mutation)
    {
        var token = await Issue();
        _record = mutation switch
        {
            "long-lifetime" => _record! with { ExpiresAt = Now.AddDays(90).AddSeconds(1) },
            "expired" => _record! with { ExpiresAt = Now },
            "future" => _record! with { CreatedAt = Now.AddSeconds(1) },
            "resource" => _record! with { Resource = "resource-b" },
            "audience" => _record! with { Audience = "https://resource-b.example" },
            "account" => _record! with { AccountKey = new("", "not-a-subject") },
            "organization" => _record! with { Organisation = new("") },
            "environment" => _record! with { Environment = "elsewhere" },
            "digest" => _record! with { Verifier = new string('0', 64) },
            "scope" => _record! with { Scopes = new HashSet<string> { "admin" } },
            "assurance" => _record! with { Assurance = new("", 0, null, null) },
            "old-epoch" => _record! with { RecoveryEpoch = 3 },
            "missing-epoch" => _record! with { RecoveryEpoch = null },
            "future-epoch" => _record! with { RecoveryEpoch = 5 },
            "missing" => null,
            _ => _record
        };
        _revoked = mutation == "revoked";
        _policyCalls = 0;
        (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
        _policyCalls.ShouldEqual(0);
    }

    [Theory]
    [InlineData("epoch-throws")]
    [InlineData("authority-throws")]
    [InlineData("epoch-unavailable")]
    public async Task should_refuse_authority_or_epoch_outages_without_calling_policy(string outage)
    {
        var token = await Issue();
        if (outage == "epoch-throws") _epochs.Read(Arg.Any<CancellationToken>()).Returns<Task<ulong?>>(_ => throw new IOException("Synthetic epoch outage."));
        else if (outage == "epoch-unavailable") _epochs.Read(Arg.Any<CancellationToken>()).Returns((ulong?)null);
        else _authority.ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Task<PatRecord?>>(_ => throw new IOException("Synthetic authority outage."));
        _policyCalls = 0;
        (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
        _policyCalls.ShouldEqual(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reevaluate_a_stale_policy_epoch_against_current_policy(bool permit)
    {
        var token = await Issue();
        _record = _record! with { PolicyEpoch = 1 };
        _policy.Evaluate(Arg.Any<PatRecord>(), false, Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(permit, TimeSpan.FromDays(90), 8));
        var result = await Source().Verify(Presentation(token), TestContext.Current.CancellationToken);
        (result.Facts is not null).ShouldEqual(permit);
        if (permit) result.Facts!.PolicyEpoch.ShouldEqual(8UL);
    }

    [Fact]
    public async Task should_refuse_current_policy_denials()
    {
        var token = await Issue();
        _policy.Evaluate(Arg.Any<PatRecord>(), false, Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(false, TimeSpan.FromDays(90), 8));
        (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
    }

    [Fact]
    public async Task should_refuse_policy_outages()
    {
        var token = await Issue();
        _policy.Evaluate(Arg.Any<PatRecord>(), false, Arg.Any<CancellationToken>()).Returns<Task<PatPolicyDecision>>(_ => throw new IOException("Synthetic policy outage."));
        (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
    }

    [Fact]
    public async Task should_apply_a_shorter_current_verification_cap()
    {
        var token = await Issue();
        _policy.Evaluate(Arg.Any<PatRecord>(), false, Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(true, TimeSpan.FromDays(30), 8));
        (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
    }

    [Fact]
    public async Task should_refuse_at_another_resource_even_when_the_authority_returns_an_active_record()
    {
        var token = await Issue();
        var settings = PatSettings.Create(new("studio") { AllowedScopes = ["read"] }, new("resource-b", "https://resource-b.example", "test"));
        var other = new PatCredentialSource(settings, _authority, _epochs, _policy, _clock);
        (await other.Verify(Presentation(token), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
    }

    [Fact]
    public async Task should_not_claim_an_oauth_value()
    {
        (await Source().Verify(Presentation("plain-oauth-fixture"), TestContext.Current.CancellationToken)).Applicable.ShouldBeFalse();
    }
}
