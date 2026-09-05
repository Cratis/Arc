// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

/// <summary>
/// The blank guard rejects a field that is nothing but whitespace - it does not trim one that merely carries some.
/// </summary>
/// <remarks>
/// This is the control for <c>and_the_identity_provider_is_blank</c>: without it, a handler that stopped writing the
/// claim altogether would satisfy every "should not expose" assertion there. It also pins the documented promise that
/// the value is the forwarded field verbatim, which a well meaning <c>Trim()</c> would silently break.
/// </remarks>
public class and_the_identity_provider_is_padded_with_whitespace : given.a_handler
{
    const string PaddedIdentityProvider = "  aad  ";

    AuthenticateResult _result;

    async Task Because() => _result = await Authenticate(PaddedIdentityProvider, (BenignClaimType, BenignClaimValue));

    [Fact] void should_authenticate() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_expose_exactly_one_identity_provider() => IdentityProviderClaimsFrom(_result).Length.ShouldEqual(1);
    [Fact] void should_expose_the_forwarded_value_including_its_padding() => IdentityProviderClaimsFrom(_result)[0].ShouldEqual(PaddedIdentityProvider);
}
