// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_dispatching : given.a_pat_module
{
    [Theory]
    [InlineData("Bearer dpat_0123456789abcdef0123456789abcdef", false)]
    [InlineData("Bearer dpat_", false)]
    [InlineData("Bearer DPAT_MALFORMED", false)]
    [InlineData("DPoP dpat_0123456789abcdef", false)]
    [InlineData("Basic dpat_0123456789abcdef", false)]
    [InlineData("Bearer dpat_revoked", true)]
    [InlineData("Bearer dpat_expired", true)]
    public async Task should_terminally_refuse_every_reserved_legacy_presentation(string authorization, bool markers)
    {
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        var pat = Substitute.For<IVerifiedCredentialSource>();
        pat.Shape.Returns(PresentedCredentialShape.PatBearer);
        var resolution = await Resolver(authorization, markers ? [ForwardedIdentityEnvelope.Header, "X-Forwarded-User"] : [], oauth, pat).GetCurrent();
        resolution.Refusal.ShouldEqual(CallerRefusal.LegacyCredentialUnsupported);
        resolution.Caller.ShouldBeNull();
        await pat.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
        await oauth.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
        await _authority.DidNotReceive().ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _policyCalls.ShouldEqual(0);
    }

    [Theory]
    [InlineData("Bearer DIRECT_PAT_v1_")]
    [InlineData("Bearer direct_pat_v2_")]
    [InlineData("Bearer direct_pat_")]
    [InlineData("DPoP direct_pat_v1_")]
    [InlineData("Bearer unregistered_pat_v1_")]
    public async Task should_not_dispatch_pat_namespace_values_to_oauth(string authorization)
    {
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        (await Resolver(authorization, oauth: oauth).GetCurrent()).Caller.ShouldBeNull();
        await oauth.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_allow_new_pat_and_oauth_controls_in_the_same_installation()
    {
        var token = await Issue();
        var facts = (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Facts!;
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        oauth.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(facts with { CredentialKind = CallerCredentialKind.OAuthGrant }));
        (await Resolver("Bearer " + token, oauth: oauth).GetCurrent()).Caller.ShouldNotBeNull();
        await oauth.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
        (await Resolver("Bearer plain-oauth-fixture", oauth: oauth).GetCurrent()).Caller.ShouldNotBeNull();
        await oauth.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("access_token")]
    [InlineData("accessToken")]
    [InlineData("id_token")]
    [InlineData("refresh_token")]
    [InlineData("token")]
    [InlineData("bearer")]
    [InlineData("pat")]
    [InlineData("session")]
    [InlineData("session_token")]
    [InlineData("credential")]
    [InlineData("authorization")]
    [InlineData("%61ccess%5Ftoken")]
    [InlineData("other")]
    [InlineData("envelope")]
    public async Task should_refuse_known_and_host_added_retired_formats_on_every_arrival_path_without_fallback(string arrival)
    {
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        var pat = Substitute.For<IVerifiedCredentialSource>();
        pat.Shape.Returns(PresentedCredentialShape.PatBearer);
        foreach (var prefix in ReservedCredentialPrefixes.KnownRetiredPrefixes.Concat(["hostlegacy"]))
        {
            var token = prefix == "dpat" ? $"dpat_{new string('a', 32)}_{new string('b', 64)}" : $"{prefix}_synthetic-opaque";
            PatNamespace.Classify(token).ShouldEqual(PresentedCredentialShape.OAuthBearer);
            PatFormat.TryParse(token, out _).ShouldBeFalse();
            var authorization = arrival == "Authorization" ? "Bearer " + token : string.Empty;
            _rawTarget = arrival == "Authorization" || arrival == "envelope" ? "/mcp" : $"/mcp?{arrival}={Uri.EscapeDataString("Bearer\t" + token)}";
            _envelope = arrival == "envelope" ? token : null;
            var result = await Resolver(authorization, oauth: oauth, pat: pat, reserved: prefix == "hostlegacy" ? ["hostlegacy_"] : null).GetCurrent();
            result.Refusal.ShouldEqual(CallerRefusal.LegacyCredentialUnsupported);
            result.Caller.ShouldBeNull();
        }
        await oauth.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
        await pat.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
        await _authority.DidNotReceive().ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _policyCalls.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_a_corrupted_secret_without_oauth_fallback()
    {
        var token = await Issue();
        var corrupted = token[..^1] + (token[^1] == 'A' ? 'Q' : 'A');
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        (await Resolver("Bearer " + corrupted, oauth: oauth).GetCurrent()).Caller.ShouldBeNull();
        await oauth.DidNotReceive().Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_not_reserve_values_without_the_retired_prefix_delimiter()
    {
        var token = await Issue();
        var facts = (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Facts!;
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        oauth.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(facts with { CredentialKind = CallerCredentialKind.OAuthGrant }));
        (await Resolver("Bearer dpatient", oauth: oauth).GetCurrent()).Caller.ShouldNotBeNull();
        await oauth.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_refuse_the_next_request_after_revocation_despite_a_stale_positive_response()
    {
        var token = await Issue();
        var stale = await Resolver("Bearer " + token).GetCurrent();
        stale.Caller.ShouldNotBeNull();
        _caller = stale.Caller!;
        (await _tokens.Revoke(_record!.CredentialId, TestContext.Current.CancellationToken)).ShouldEqual(PatRevokeResult.Revoked);
        (await _tokens.Revoke(_record.CredentialId, TestContext.Current.CancellationToken)).ShouldEqual(PatRevokeResult.AlreadyRevoked);
        (await Resolver("Bearer " + token).GetCurrent()).Caller.ShouldBeNull();
        await _authority.Received(2).ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("person")]
    [InlineData("tenant")]
    [InlineData("account")]
    public async Task should_refuse_revocation_outside_the_complete_verified_owner_binding(string mismatch)
    {
        var token = await Issue();
        _caller = (await Resolver("Bearer " + token).GetCurrent()).Caller!;
        _caller = mismatch switch
        {
            "person" => _caller with { Person = new("another-person") },
            "tenant" => _caller with { Tenant = new("another-tenant") },
            _ => _caller with { AccountKey = new("https://other-identity.example", _account.Subject) }
        };
        (await _tokens.Revoke(_record!.CredentialId, TestContext.Current.CancellationToken)).ShouldEqual(PatRevokeResult.Refused);
        _revoked.ShouldBeFalse();
    }
}
