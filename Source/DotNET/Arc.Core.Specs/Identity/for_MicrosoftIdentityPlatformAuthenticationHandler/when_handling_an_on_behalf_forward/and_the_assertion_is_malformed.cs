// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_malformed : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), "not-a-jws", Tenant));

    [Fact] void should_not_authenticate_the_request() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_forward() => _result.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.MalformedAssertion);
}
