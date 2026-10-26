// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_an_on_behalf_header_is_repeated : given.an_on_behalf_forward
{
    AuthenticationResult _repeatedActor;
    AuthenticationResult _repeatedAssertion;

    async Task Because()
    {
        var actor = ActorHeaderValue();
        var assertion = OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actor));
        _repeatedActor = await _handler.HandleAuthentication(ContextForOnBehalfOf($"{actor},{ActorHeaderValue("another-actor")}", assertion, Tenant));
        _repeatedAssertion = await _handler.HandleAuthentication(ContextForOnBehalfOf(actor, $"{assertion},{assertion}", Tenant));
    }

    [Fact] void should_reject_a_repeated_actor() => _repeatedActor.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.RepeatedHeader);
    [Fact] void should_reject_a_repeated_assertion() => _repeatedAssertion.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.RepeatedHeader);
    [Fact] void should_not_authenticate_either() => new[] { _repeatedActor, _repeatedAssertion }.Any(_ => _.IsAuthenticated).ShouldBeFalse();
}
