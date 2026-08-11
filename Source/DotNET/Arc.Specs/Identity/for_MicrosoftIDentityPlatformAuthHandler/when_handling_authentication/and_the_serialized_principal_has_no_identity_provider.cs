// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_the_serialized_principal_has_no_identity_provider : given.a_handler
{
    AuthenticateResult _result;

    async Task Because() => _result = await Authenticate(null, (BenignClaimType, BenignClaimValue));

    [Fact] void should_authenticate() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_claim() => ClaimValuesFrom(_result, BenignClaimType).ShouldContainOnly(BenignClaimValue);
    [Fact] void should_not_expose_an_identity_provider() => IdentityProviderClaimsFrom(_result).ShouldBeEmpty();
}
