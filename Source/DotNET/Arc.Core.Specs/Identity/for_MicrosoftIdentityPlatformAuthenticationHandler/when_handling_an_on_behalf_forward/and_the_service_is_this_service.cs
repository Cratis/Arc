// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_service_is_this_service : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    void Establish() => _arcOptions.OnBehalfOf.Service = "portal";

    async Task Because() => _result = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["svc"] = "portal"));

    [Fact] void should_authenticate_the_request() => _result.IsAuthenticated.ShouldBeTrue();
}
