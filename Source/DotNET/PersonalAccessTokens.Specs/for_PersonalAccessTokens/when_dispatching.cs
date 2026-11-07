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
    public async Task should_not_reserve_legacy_namespaces_unless_explicitly_configured()
    {
        var token = await Issue();
        var facts = (await Source().Verify(Presentation(token), TestContext.Current.CancellationToken)).Facts!;
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        oauth.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(facts with { CredentialKind = CallerCredentialKind.OAuthGrant }));
        (await Resolver("Bearer dpat_example", oauth: oauth, reserved: []).GetCurrent()).Caller.ShouldNotBeNull();
        await oauth.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_refuse_the_next_request_after_revocation_despite_a_stale_positive_response()
    {
        var token = await Issue();
        var stale = await Resolver("Bearer " + token).GetCurrent();
        stale.Caller.ShouldNotBeNull();
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
        await Issue();
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
