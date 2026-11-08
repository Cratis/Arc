// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_composing_issuance_and_pat_callers : given.a_composed_installation
{
    [Fact]
    public async Task should_issue_from_a_verified_signed_browser_and_resolve_the_pat()
    {
        var issued = await IssueWith(Browser());
        issued.RefusalCode.ShouldBeNull();
        using var token = issued.Token!;
        var caller = (await ResolveWith(new() { ["Authorization"] = ["Bearer " + token.Reveal()] })).Caller!;
        caller.CredentialKind.ShouldEqual(CallerCredentialKind.PersonalAccessToken);
        caller.Person.ShouldEqual(_person);
        caller.AccountKey.ShouldEqual(_account);
        caller.Tenant.ShouldEqual(_tenant);
        caller.Scopes.ShouldEqual(["read"]);
        _persons.Calls.ShouldEqual(1);
        _creates.ShouldEqual(1);
    }

    [Fact]
    public async Task should_not_allow_a_pat_to_issue_another_pat_and_should_reread_revocation()
    {
        using var token = (await IssueWith(Browser())).Token!;
        var headers = new Dictionary<string, string[]> { ["Authorization"] = ["Bearer " + token.Reveal()] };
        (await IssueWith(headers)).Token.ShouldBeNull();
        _creates.ShouldEqual(1);
        (await ResolveWith(headers)).Caller.ShouldNotBeNull();
        _revoked = true;
        (await ResolveWith(headers)).Caller.ShouldBeNull();
    }

    [Theory]
    [InlineData("recovery advance")]
    [InlineData("policy deny")]
    [InlineData("authority unavailable")]
    [InlineData("recovery unavailable")]
    [InlineData("expired")]
    public async Task should_reread_current_authorities_for_each_pat_request(string change)
    {
        using var token = (await IssueWith(Browser())).Token!;
        var headers = new Dictionary<string, string[]> { ["Authorization"] = ["Bearer " + token.Reveal()] };
        (await ResolveWith(headers)).Caller.ShouldNotBeNull();
        switch (change)
        {
            case "recovery advance": _epochs.Read(Arg.Any<CancellationToken>()).Returns(5UL); break;
            case "policy deny": _policy.Evaluate(Arg.Any<PatRecord>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new PatPolicyDecision(false, TimeSpan.Zero, 8)); break;
            case "authority unavailable": _authority.ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<PatRecord?>(_ => throw new InvalidOperationException("unavailable")); break;
            case "recovery unavailable": _epochs.Read(Arg.Any<CancellationToken>()).Returns<ulong?>(_ => throw new InvalidOperationException("unavailable")); break;
            case "expired": _clock.GetUtcNow().Returns(Now.AddDays(2)); break;
        }
        (await ResolveWith(headers)).Caller.ShouldBeNull();
        _creates.ShouldEqual(1);
    }

    [Theory]
    [InlineData("unsigned")]
    [InlineData("v1")]
    [InlineData("tampered")]
    [InlineData("wrong audience")]
    [InlineData("wrong tenant")]
    [InlineData("expired")]
    [InlineData("missing hop")]
    [InlineData("duplicate authorization")]
    [InlineData("cookie and bearer")]
    [InlineData("retired")]
    [InlineData("uri")]
    public async Task should_refuse_hostile_issuance_without_creating_a_record(string attack)
    {
        var headers = Browser(attack == "wrong audience" ? "other-resource" : "resource-a", attack == "wrong tenant" ? "tenant-other" : "tenant-one", attack == "expired" ? 600 : 0);
        ApplyAttack(headers, attack);
        (await IssueWith(headers, attack != "missing hop", attack == "uri" ? "/identity/pat?access_token=bad" : "/identity/pat")).Token.ShouldBeNull();
        _creates.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_replayed_issuance()
    {
        var headers = Browser();
        using var issued = (await IssueWith(headers)).Token;
        issued.ShouldNotBeNull();
        (await IssueWith(headers)).Token.ShouldBeNull();
        _creates.ShouldEqual(1);
    }

    [Fact]
    public async Task should_verify_a_forwarded_pat_with_its_authority_without_allowing_issuance()
    {
        using var token = (await IssueWith(Browser())).Token!;
        var headers = ForwardedPat(token);
        (await ResolveWith(headers)).Caller!.CredentialKind.ShouldEqual(CallerCredentialKind.PersonalAccessToken);
        Sign(headers);
        (await IssueWith(headers)).Token.ShouldBeNull();
        _creates.ShouldEqual(1);
        _revoked = true;
        Sign(headers);
        (await ResolveWith(headers)).Caller.ShouldBeNull();
        await _authority.Received(3).ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("browser-session")]
    [InlineData("oauth-grant")]
    public async Task should_not_turn_a_forwarded_pat_into_an_issuance_credential(string claimedKind)
    {
        using var token = (await IssueWith(Browser())).Token!;
        var headers = ForwardedPat(token);
        headers["x-cratis-credential-kind"] = [claimedKind];
        Sign(headers);
        (await IssueWith(headers)).Token.ShouldBeNull();
        _creates.ShouldEqual(1);
    }

    Dictionary<string, string[]> ForwardedPat(IssuedPat token)
    {
        var headers = Browser();
        var rawToken = token.Reveal();
        headers["Authorization"] = ["Bearer " + rawToken];
        headers["x-cratis-credential-kind"] = ["pat"];
        headers["x-cratis-credential-id"] = [_record!.CredentialId.Value];
        headers["x-cratis-forwarded-token-sha256"] = [UrlTokenHash(rawToken)];
        Sign(headers);
        return headers;
    }

    static void ApplyAttack(Dictionary<string, string[]> headers, string attack)
    {
        switch (attack)
        {
            case "unsigned": headers.Remove("x-cratis-forwarded-identity-signature"); break;
            case "v1": headers["x-cratis-forwarded-identity-signature"] = ["v1.test.invalid"]; break;
            case "tampered": headers["x-cratis-subject"] = ["dGFtcGVyZWQ"]; break;
            case "duplicate authorization": headers["Authorization"] = ["Bearer direct_pat_bad", "Bearer other"]; break;
            case "cookie and bearer": headers.Remove("x-cratis-forwarded-identity-signature"); headers["Authorization"] = ["Bearer direct_pat_bad"]; break;
            case "retired": headers["Authorization"] = ["Bearer dpat_bad"]; break;
        }
    }

    static string UrlTokenHash(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Fact]
    public void should_register_exactly_the_two_intended_sources()
    {
        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetServices<IVerifiedCredentialSource>().Select(source => source.Shape).Order().ShouldEqual(new[] { PresentedCredentialShape.PatBearer, PresentedCredentialShape.Envelope }.Order());
    }
}
