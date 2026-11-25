// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Delegation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_delegation_allows_a_verified_caller : given.a_delegated_consumer
{
    DelegationRequest _observed;

    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(Arg.Any<DelegationRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _observed = call.Arg<DelegationRequest>();
            return true;
        });
    }

    Task Because() => InvokeConsumer();

    [Fact] void should_reach_the_separate_product_authorization_path() => _result.ShouldEqual("product-authorized");
    [Fact] void should_preserve_the_verified_caller_for_the_authority() => ReferenceEquals(_observed.Caller, _caller).ShouldBeTrue();
    [Fact] void should_preserve_the_verified_caller_for_the_operation() => ReferenceEquals(_operationCaller, _caller).ShouldBeTrue();
    [Fact] void should_preserve_the_credential_kind() => _observed.Caller.CredentialKind.ShouldEqual(CallerCredentialKind.OAuthGrant);
    [Fact] void should_preserve_the_credential_id() => _observed.Caller.CredentialId.Value.ShouldEqual("credential-fixture");
    [Fact] void should_preserve_the_grant_reference() => _observed.Caller.GrantId!.Value.ShouldEqual("grant-fixture");
}
