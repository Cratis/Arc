// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_an_on_behalf_forward;

public class and_the_forward_is_trusted : given.an_on_behalf_forward
{
    AuthenticateResult _result;
    ClaimsIdentity? _actor;

    async Task Because()
    {
        _result = await AuthenticateOnBehalfOf(ActorHeaderValue());
        _actor = _result.Principal?.GetActor();
    }

    [Fact] void should_authenticate_the_request() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_make_the_represented_user_the_current_user() => _result.Principal!.FindFirst("sub")!.Value.ShouldEqual(IdentityIdFromHeader);
    [Fact] void should_authorize_with_the_represented_user_roles() => _result.Principal!.IsInRole(RepresentedRole).ShouldBeTrue();
    [Fact] void should_not_authorize_with_the_actor_roles() => _result.Principal!.IsInRole(ActorRole).ShouldBeFalse();
    [Fact] void should_expose_the_actor_subject() => _actor!.FindFirst("sub")!.Value.ShouldEqual(ActorId);
    [Fact] void should_expose_the_actor_name() => _actor!.Name.ShouldEqual(ActorDetails);
    [Fact] void should_authenticate_the_actor_with_the_scheme() => _actor!.AuthenticationType.ShouldEqual(MicrosoftIDentityPlatformAuthHandler.SchemeName);
}
