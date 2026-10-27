// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_service_is_another : given.an_on_behalf_forward
{
    AuthenticationResult _anotherService;
    AuthenticationResult _noService;

    void Establish() => _arcOptions.OnBehalfOf.Service = "portal";

    async Task Because()
    {
        _anotherService = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["svc"] = "billing"));
        _noService = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue()));
    }

    [Fact] void should_reject_another_service() => _anotherService.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.ServiceMismatch);
    [Fact] void should_reject_an_assertion_without_a_service() => _noService.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.ServiceMismatch);
}
