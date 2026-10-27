// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_does_not_verify : given.an_on_behalf_forward
{
    AuthenticateResult _missing;
    AuthenticateResult _forged;
    AuthenticateResult _swappedActor;

    async Task Because()
    {
        var actor = ActorHeaderValue();
        _missing = await AuthenticateOnBehalfOf(actor, StringValues.Empty);
        _forged = await AuthenticateOnBehalfOf(actor, OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actor), key: OnBehalfOfAssertionsForSpecs.UnknownKey));
        _swappedActor = await AuthenticateOnBehalfOf(ActorHeaderValue("another-actor"), OnBehalfOfAssertionsForSpecs.Sign(ClaimsFor(actor)));
    }

    [Fact] void should_reject_a_forward_without_an_assertion() => _missing.Failure!.Message.ShouldEqual(OnBehalfOfFailures.MissingAssertion);
    [Fact] void should_reject_an_assertion_not_signed_by_a_configured_key() => _forged.Failure!.Message.ShouldEqual(OnBehalfOfFailures.InvalidSignature);
    [Fact] void should_reject_an_actor_the_assertion_does_not_cover() => _swappedActor.Failure!.Message.ShouldEqual(OnBehalfOfFailures.ActorMismatch);
}
