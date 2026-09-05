// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_the_colliding_claim_only_differs_in_casing : given.a_handler
{
    const string IdentityProviderFromIngress = "aad";
    const string CaseVariedClaimType = "URN:CRATIS:ARC:Identity:Provider";
    const string ForgedIdentityProvider = "forged-by-the-caller";

    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(
        ContextForPrincipal(
            IdentityProviderFromIngress,
            (CaseVariedClaimType, ForgedIdentityProvider),
            (BenignClaimType, BenignClaimValue)));

    [Fact] void should_authenticate() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_result, BenignClaimType).ShouldContainOnly(BenignClaimValue);
    [Fact] void should_expose_exactly_one_identity_provider() => IdentityProviderClaimsFrom(_result).Length.ShouldEqual(1);
    [Fact] void should_expose_the_identity_provider_the_ingress_supplied() => IdentityProviderClaimsFrom(_result)[0].ShouldEqual(IdentityProviderFromIngress);
    [Fact] void should_strip_the_case_varied_forgery() => IdentityProviderClaimsFrom(_result).ShouldNotContain(ForgedIdentityProvider);
}
