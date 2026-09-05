// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_the_serialized_principal_carries_the_ingress_authored_provider_key_claim : given.a_handler
{
    const string IdentityProviderFromIngress = "aad";

    AuthenticateResult _result;

    async Task Because() => _result = await Authenticate(
        IdentityProviderFromIngress,
        (IngressProviderKeyClaimType, IngressProviderKeyClaimValue));

    [Fact] void should_authenticate() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_leave_the_provider_key_claim_the_ingress_authored_untouched() => ClaimValuesFrom(_result, IngressProviderKeyClaimType).ShouldContainOnly(IngressProviderKeyClaimValue);
    [Fact] void should_expose_exactly_one_identity_provider() => IdentityProviderClaimsFrom(_result).Length.ShouldEqual(1);
    [Fact] void should_expose_the_identity_provider_the_ingress_supplied() => IdentityProviderClaimsFrom(_result)[0].ShouldEqual(IdentityProviderFromIngress);
}
