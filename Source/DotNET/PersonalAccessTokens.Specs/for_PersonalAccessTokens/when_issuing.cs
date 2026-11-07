// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_issuing : given.a_pat_module
{
    [Fact]
    public async Task should_commit_only_a_one_way_verifier_and_intent_before_revealing_once()
    {
        var result = await _tokens.Issue(TimeSpan.FromDays(90), ["read"], TestContext.Current.CancellationToken);
        _creates.ShouldEqual(1);
        _record!.ExpiresAt.ShouldEqual(new DateTimeOffset(2030, 4, 1, 0, 0, 0, TimeSpan.Zero));
        _record.Assurance.AuthTime.ShouldEqual(_caller.Assurance!.AuthTime);
        _record.Assurance.Idp.ShouldEqual(_caller.Assurance.Idp);
        _record.Assurance.Acr.ShouldEqual("basic");
        _record.RecoveryEpoch.ShouldEqual(4UL);
        var raw = result.Token!.Reveal()!;
        PatFormat.TryParse(raw, out _).ShouldBeTrue();
        _record.Verifier.ShouldEqual(PatVerifier.Derive(raw));
        JsonSerializer.Serialize(_record).Contains(raw, StringComparison.Ordinal).ShouldBeFalse();
        JsonSerializer.Serialize(_intent).Contains(raw, StringComparison.Ordinal).ShouldBeFalse();
        result.Token.Reveal().ShouldBeNull();
        result.ToString().Contains(raw, StringComparison.Ordinal).ShouldBeFalse();
        JsonSerializer.Serialize(result).Contains(raw, StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(7776001)]
    public async Task should_refuse_invalid_lifetimes_before_committing(int seconds)
    {
        var result = await _tokens.Issue(TimeSpan.FromSeconds(seconds), ["read"], TestContext.Current.CancellationToken);
        result.Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("*")]
    [InlineData("admin")]
    [InlineData("approve")]
    public async Task should_refuse_unavailable_scopes(string scope)
    {
        (await _tokens.Issue(TimeSpan.FromDays(1), [scope], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Fact]
    public async Task should_apply_a_shorter_current_policy_cap()
    {
        _policy.Evaluate(Arg.Any<PatRecord>(), true, Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(true, TimeSpan.FromDays(30), 7));
        (await _tokens.Issue(TimeSpan.FromDays(90), ["read"], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Theory]
    [InlineData("assurance")]
    [InlineData("tenant")]
    [InlineData("account")]
    [InlineData("person")]
    [InlineData("scope-ceiling")]
    [InlineData("pat-cannot-mint")]
    public async Task should_require_verified_issuance_context(string missing)
    {
        _caller = missing switch
        {
            "assurance" => _caller with { Assurance = null },
            "tenant" => _caller with { Tenant = null },
            "account" => _caller with { AccountKey = null },
            "person" => _caller with { Person = null },
            "scope-ceiling" => _caller with { Scopes = new HashSet<string>() },
            _ => _caller with { CredentialKind = CallerCredentialKind.PersonalAccessToken }
        };
        (await _tokens.Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Fact]
    public async Task should_lose_the_secret_when_the_commit_response_is_lost()
    {
        _authority.TryCreate(Arg.Any<PatRecord>(), Arg.Any<PatIssuedIntent>(), Arg.Any<CancellationToken>()).Returns<bool>(call =>
        {
            _record = call.Arg<PatRecord>();
            throw new IOException("Synthetic commit response failure.");
        });
        (await _tokens.Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        _record.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("recovery-unavailable")]
    [InlineData("recovery-throws")]
    [InlineData("policy-denied")]
    [InlineData("policy-throws")]
    public async Task should_refuse_before_generation_and_commit_when_current_checks_fail(string failure)
    {
        if (failure == "recovery-unavailable") _epochs.Read(Arg.Any<CancellationToken>()).Returns((ulong?)null);
        else if (failure == "recovery-throws") _epochs.Read(Arg.Any<CancellationToken>()).Returns<Task<ulong?>>(_ => throw new IOException("Synthetic epoch outage."));
        else if (failure == "policy-denied") _policy.Evaluate(Arg.Any<PatRecord>(), true, Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(false, TimeSpan.FromDays(90), 7));
        else _policy.Evaluate(Arg.Any<PatRecord>(), true, Arg.Any<CancellationToken>()).Returns<Task<PatPolicyDecision>>(_ => throw new IOException("Synthetic policy outage."));
        (await _tokens.Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Fact]
    public async Task should_never_reveal_before_the_atomic_commit_is_acknowledged()
    {
        var pending = new TaskCompletionSource<bool>();
        _authority.TryCreate(Arg.Any<PatRecord>(), Arg.Any<PatIssuedIntent>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var issue = _tokens.Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken);
        issue.IsCompleted.ShouldBeFalse();
        pending.SetResult(false);
        (await issue).Token.ShouldBeNull();
    }
}
