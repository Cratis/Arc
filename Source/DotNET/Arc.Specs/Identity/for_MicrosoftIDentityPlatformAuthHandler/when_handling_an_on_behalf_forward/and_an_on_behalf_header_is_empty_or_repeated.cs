// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_an_on_behalf_header_is_empty_or_repeated : given.an_on_behalf_forward
{
    AuthenticateResult _emptyActor;
    AuthenticateResult _repeatedActor;
    AuthenticateResult _repeatedAssertion;

    async Task Because()
    {
        var actor = ActorHeaderValue();
        var assertion = OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actor));
        _emptyActor = await AuthenticateOnBehalfOf(string.Empty, OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(string.Empty)));
        _repeatedActor = await AuthenticateOnBehalfOf(new StringValues([actor, ActorHeaderValue("another-actor")]), assertion);
        _repeatedAssertion = await AuthenticateOnBehalfOf(actor, new StringValues([assertion, assertion]));
    }

    [Fact] void should_reject_an_empty_actor() => _emptyActor.Failure!.Message.ShouldEqual(OnBehalfOfFailures.MissingActor);
    [Fact] void should_reject_a_repeated_actor() => _repeatedActor.Failure!.Message.ShouldEqual(OnBehalfOfFailures.RepeatedHeader);
    [Fact] void should_reject_a_repeated_assertion() => _repeatedAssertion.Failure!.Message.ShouldEqual(OnBehalfOfFailures.RepeatedHeader);
}
