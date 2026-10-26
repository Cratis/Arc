// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_replayed : given.an_on_behalf_forward
{
    AuthenticationResult _first;
    AuthenticationResult _replay;

    async Task Because()
    {
        var actor = ActorHeaderValue();
        var assertion = OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actor));
        _first = await _handler.HandleAuthentication(ContextForOnBehalfOf(actor, assertion, Tenant));
        _replay = await _handler.HandleAuthentication(ContextForOnBehalfOf(actor, assertion, Tenant));
    }

    [Fact] void should_accept_the_first_use() => _first.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_authenticate_the_replay() => _replay.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_replay() => _replay.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.Replayed);
}
