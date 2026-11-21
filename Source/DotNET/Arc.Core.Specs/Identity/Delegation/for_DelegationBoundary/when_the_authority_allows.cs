// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_the_authority_allows : given.a_boundary
{
    DelegationRequest _observed;

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(_request, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _observed = call.Arg<DelegationRequest>();
            return true;
        });
    }

    async Task Because() => _result = await CreateBoundary().IsAllowed(_request);

    [Fact] void should_allow() => _result.ShouldBeTrue();
    [Fact] void should_forward_the_original_request() => ReferenceEquals(_observed, _request).ShouldBeTrue();
    [Fact] void should_forward_the_original_caller() => ReferenceEquals(_observed.Caller, _caller).ShouldBeTrue();
    [Fact] void should_preserve_the_credential_kind() => _observed.Caller.CredentialKind.ShouldEqual(CallerCredentialKind.OAuthGrant);
    [Fact] void should_preserve_the_credential_id() => _observed.Caller.CredentialId.Value.ShouldEqual("credential-fixture");
    [Fact] void should_preserve_the_verified_grant() => _observed.Caller.GrantId!.Value.ShouldEqual("grant-fixture");
}
