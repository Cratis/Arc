// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_bound_to_the_path_behind_a_path_base : given.an_on_behalf_forward
{
    AuthenticateResult _fullPath;
    AuthenticateResult _encodedAsAuthProxyDoes;
    AuthenticateResult _pathWithoutBase;
    AuthenticateResult _withHost;

    void Establish()
    {
        _pathBase = "/portal";
        _path = "/api/tickets/søren";
    }

    async Task Because()
    {
        _fullPath = await AuthenticateOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "/portal/api/tickets/søren");
        _encodedAsAuthProxyDoes = await AuthenticateOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "/portal/api/tickets/s%C3%B8ren");
        _pathWithoutBase = await AuthenticateOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "/api/tickets/s%C3%B8ren");
        _withHost = await AuthenticateOnBehalfOf(ActorHeaderValue(), claims => claims["htu"] = "https://app.example.com/portal/api/tickets/s%C3%B8ren");
    }

    [Fact] void should_authenticate_the_path_base_and_path() => _fullPath.Succeeded.ShouldBeTrue();
    [Fact] void should_authenticate_the_percent_encoded_path() => _encodedAsAuthProxyDoes.Succeeded.ShouldBeTrue();
    [Fact] void should_reject_the_path_without_its_base() => _pathWithoutBase.Failure!.Message.ShouldEqual(OnBehalfOfFailures.TargetMismatch);
    [Fact] void should_reject_a_target_with_a_host() => _withHost.Failure!.Message.ShouldEqual(OnBehalfOfFailures.TargetMismatch);
}
