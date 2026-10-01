// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_forwarded_identity_headers_are_not_trusted : given.a_handler
{
    AuthenticateResult _result;

    void Establish() => _arcOptions.TrustForwardedIdentityHeaders = false;

    async Task Because() => _result = await Authenticate("aad", (BenignClaimType, BenignClaimValue));

    [Fact] void should_not_authenticate_the_request() => _result.Succeeded.ShouldBeFalse();
    [Fact] void should_return_no_result() => _result.None.ShouldBeTrue();
}
