// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_actor_is_malformed : given.an_on_behalf_forward
{
    AuthenticationResult _notBase64;
    AuthenticationResult _notJson;
    AuthenticationResult _withoutUserId;

    async Task Because()
    {
        _notBase64 = await _handler.HandleAuthentication(ContextForOnBehalfOf("not base64!"));
        _notJson = await _handler.HandleAuthentication(ContextForOnBehalfOf(Convert.ToBase64String("not json"u8.ToArray())));
        _withoutUserId = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(string.Empty)));
    }

    [Fact] void should_reject_an_actor_that_is_not_base64() => _notBase64.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.InvalidActor);
    [Fact] void should_reject_an_actor_that_is_not_json() => _notJson.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.InvalidActor);
    [Fact] void should_reject_an_actor_without_a_user_id() => _withoutUserId.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.InvalidActor);
    [Fact] void should_not_authenticate_any_of_them() => new[] { _notBase64, _notJson, _withoutUserId }.Any(_ => _.IsAuthenticated).ShouldBeFalse();
}
