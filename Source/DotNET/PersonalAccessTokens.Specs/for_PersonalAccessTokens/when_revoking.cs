// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_revoking : given.a_pat_module
{
    [Theory]
    [InlineData(CallerCredentialKind.OAuthGrant)]
    [InlineData(CallerCredentialKind.MachinePrincipal)]
    [InlineData(CallerCredentialKind.BrowserSession)]
    [InlineData(CallerCredentialKind.OnBehalfSession)]
    [InlineData(CallerCredentialKind.Workload)]
    public async Task should_refuse_non_pat_callers_before_contacting_the_authority(CallerCredentialKind kind)
    {
        await Issue();
        _caller = _caller with { CredentialKind = kind };
        (await _tokens.Revoke(_record!.CredentialId, TestContext.Current.CancellationToken)).ShouldEqual(PatRevokeResult.Refused);
        _revoked.ShouldBeFalse();
        await _authority.DidNotReceive().TryRevoke(Arg.Any<PatOwner>(), Arg.Any<CredentialId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_refuse_an_unknown_credential_id_without_revoking_another_credential()
    {
        var token = await Issue();
        _caller = (await Resolver("Bearer " + token).GetCurrent()).Caller!;
        var unknown = new CredentialId("unknown-credential");
        (await _tokens.Revoke(unknown, TestContext.Current.CancellationToken)).ShouldEqual(PatRevokeResult.Refused);
        _revoked.ShouldBeFalse();
        await _authority.Received(1).TryRevoke(Arg.Any<PatOwner>(), unknown, Arg.Any<CancellationToken>());
        (await Resolver("Bearer " + token).GetCurrent()).Caller.ShouldNotBeNull();
    }
}
