// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_two_principals_differ_only_by_identity_provider : given.a_handler
{
    const string FirstIdentityProvider = "aad";
    const string SecondIdentityProvider = "github";

    AuthenticateResult _first;
    AuthenticateResult _second;

    async Task Because()
    {
        _first = await Authenticate(FirstIdentityProvider);
        _second = await Authenticate(SecondIdentityProvider);
    }

    [Fact] void should_authenticate_the_first_principal() => _first.Succeeded.ShouldBeTrue();
    [Fact] void should_authenticate_the_second_principal() => _second.Succeeded.ShouldBeTrue();
    [Fact] void should_expose_exactly_one_identity_provider_for_the_first_principal() => IdentityProviderClaimsFrom(_first).Length.ShouldEqual(1);
    [Fact] void should_expose_exactly_one_identity_provider_for_the_second_principal() => IdentityProviderClaimsFrom(_second).Length.ShouldEqual(1);
    [Fact] void should_expose_the_exact_first_identity_provider() => IdentityProviderClaimsFrom(_first)[0].ShouldEqual(FirstIdentityProvider);
    [Fact] void should_expose_the_exact_second_identity_provider() => IdentityProviderClaimsFrom(_second)[0].ShouldEqual(SecondIdentityProvider);
    [Fact] void should_keep_the_identity_id_from_the_header() => ClaimValuesFrom(_first, "sub")[0].ShouldEqual(IdentityIdFromHeader);
    [Fact] void should_keep_the_authentication_type_as_the_scheme_name() => _first.Principal!.Identity!.AuthenticationType.ShouldEqual(MicrosoftIDentityPlatformAuthHandler.SchemeName);
}
