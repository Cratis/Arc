// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_for_another_actor : given.an_on_behalf_forward
{
    AuthenticationResult _swappedActorHeader;
    AuthenticationResult _anotherActorSubject;

    async Task Because()
    {
        var assertion = OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(ActorHeaderValue()));
        _swappedActorHeader = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue("another-actor"), assertion, Tenant));
        _anotherActorSubject = await _handler.HandleAuthentication(ContextForOnBehalfOf(
            ActorHeaderValue(),
            claims => claims["act"] = new Dictionary<string, object> { ["sub"] = "another-actor", ["idp"] = "aad" }));
    }

    [Fact] void should_reject_an_actor_header_swapped_on_a_valid_assertion() => _swappedActorHeader.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.ActorMismatch);
    [Fact] void should_reject_an_actor_that_is_not_the_asserted_actor() => _anotherActorSubject.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.ActorMismatch);
}
