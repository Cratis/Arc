// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_forwarded_identity_headers_are_not_trusted : given.an_on_behalf_forward
{
    AuthenticateResult _result;

    void Establish() => _arcOptions.TrustForwardedIdentityHeaders = false;

    async Task Because() => _result = await AuthenticateOnBehalfOf(ActorHeaderValue());

    [Fact] void should_not_authenticate_the_request() => _result.Succeeded.ShouldBeFalse();
    [Fact] void should_fail_instead_of_returning_no_result() => _result.None.ShouldBeFalse();
    [Fact] void should_say_the_forward_is_not_trusted() => _result.Failure!.Message.ShouldEqual(ForwardedActor.UntrustedForward);
}
