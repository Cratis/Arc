// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_on_behalf_of_is_not_enabled : given.an_on_behalf_forward
{
    AuthenticationResult _withActor;
    AuthenticationResult _withOnlyAssertion;

    void Establish() => _arcOptions.OnBehalfOf.Enabled = false;

    async Task Because()
    {
        _withActor = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue()));
        _withOnlyAssertion = await _handler.HandleAuthentication(ContextForOnBehalfOf(null, OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(ActorHeaderValue())), Tenant));
    }

    [Fact] void should_not_authenticate_a_signed_forward() => _withActor.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_a_signed_forward_instead_of_ignoring_the_actor() => _withActor.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.NotEnabled);
    [Fact] void should_reject_a_request_carrying_only_the_assertion() => _withOnlyAssertion.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.NotEnabled);
}
