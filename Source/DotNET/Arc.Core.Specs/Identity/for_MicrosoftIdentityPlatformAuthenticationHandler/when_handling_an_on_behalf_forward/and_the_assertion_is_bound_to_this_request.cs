// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_bound_to_this_request : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims =>
    {
        claims["htm"] = Method;
        claims["htu"] = $"https://{Host}{Path}";
    }));

    [Fact] void should_authenticate_the_request() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_attach_the_actor() => _result.Principal!.IsActedOnBehalfOf().ShouldBeTrue();
}
