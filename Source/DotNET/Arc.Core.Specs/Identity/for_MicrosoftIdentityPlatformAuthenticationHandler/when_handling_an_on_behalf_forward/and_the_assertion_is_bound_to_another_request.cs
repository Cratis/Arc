// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_bound_to_another_request : given.an_on_behalf_forward
{
    AuthenticationResult _anotherMethod;
    AuthenticationResult _anotherPath;
    AuthenticationResult _anAbsoluteUrl;
    AuthenticationResult _aSlashEncodedPath;

    async Task Because()
    {
        _anotherMethod = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["htm"] = "DELETE"));
        _anotherPath = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "/api/tickets/delete"));
        _anAbsoluteUrl = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = $"https://app.example.com{Path}"));
        _aSlashEncodedPath = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "/api/tickets%2Fclose"));
    }

    [Fact] void should_reject_another_method() => _anotherMethod.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.MethodMismatch);
    [Fact] void should_reject_another_path() => _anotherPath.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.TargetMismatch);
    [Fact] void should_reject_an_absolute_url() => _anAbsoluteUrl.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.TargetMismatch);
    [Fact] void should_reject_a_path_with_an_encoded_slash() => _aSlashEncodedPath.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.TargetMismatch);
}
