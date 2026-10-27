// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_the_actor_is_invalid : given.an_on_behalf_forward
{
    AuthenticateResult _malformed;
    AuthenticateResult _self;

    async Task Because()
    {
        _malformed = await AuthenticateOnBehalfOf("not base64!");
        _self = await AuthenticateOnBehalfOf(ActorHeaderValue(IdentityIdFromHeader));
    }

    [Fact] void should_reject_a_malformed_actor() => _malformed.Failure!.Message.ShouldEqual(OnBehalfOfFailures.InvalidActor);
    [Fact] void should_reject_an_actor_acting_on_behalf_of_itself() => _self.Failure!.Message.ShouldEqual(OnBehalfOfFailures.ActorIsRepresentedUser);
}
