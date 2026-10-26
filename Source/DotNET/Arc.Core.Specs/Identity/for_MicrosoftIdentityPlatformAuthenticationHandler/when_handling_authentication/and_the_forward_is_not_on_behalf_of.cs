// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_the_forward_is_not_on_behalf_of : given.a_handler
{
    AuthenticationResult _result;

    async Task Because() => _result = await _handler.HandleAuthentication(ContextForPrincipal("aad"));

    [Fact] void should_authenticate_the_request() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_have_an_actor() => _result.Principal!.GetActor().ShouldBeNull();
    [Fact] void should_not_be_acted_on_behalf_of() => _result.Principal!.IsActedOnBehalfOf().ShouldBeFalse();
}
