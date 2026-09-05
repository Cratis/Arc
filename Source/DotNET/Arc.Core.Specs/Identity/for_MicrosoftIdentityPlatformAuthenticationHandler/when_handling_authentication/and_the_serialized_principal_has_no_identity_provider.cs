// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_the_serialized_principal_has_no_identity_provider : given.a_handler
{
    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(
        ContextForPrincipal(null, (BenignClaimType, BenignClaimValue)));

    [Fact] void should_authenticate() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_result, BenignClaimType).ShouldContainOnly(BenignClaimValue);
    [Fact] void should_not_expose_an_identity_provider() => IdentityProviderClaimsFrom(_result).ShouldBeEmpty();
}
