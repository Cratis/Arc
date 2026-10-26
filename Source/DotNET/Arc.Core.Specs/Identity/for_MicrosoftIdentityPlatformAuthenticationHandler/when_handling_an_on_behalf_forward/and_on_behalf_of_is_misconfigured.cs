// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_on_behalf_of_is_misconfigured : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    void Establish() => _arcOptions.OnBehalfOf.Issuer = string.Empty;

    async Task Because() => _result = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue()));

    [Fact] void should_not_authenticate_the_request() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_forward() => _result.Failure!.Reason.Value.StartsWith(OnBehalfOfFailures.Misconfigured, StringComparison.Ordinal).ShouldBeTrue();
}
