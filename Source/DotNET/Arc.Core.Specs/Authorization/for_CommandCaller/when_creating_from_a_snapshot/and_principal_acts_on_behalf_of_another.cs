// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CommandCaller.when_creating_from_a_snapshot;

public class and_principal_acts_on_behalf_of_another : Specification
{
    CommandCaller _caller;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test") { Actor = new ClaimsIdentity([new Claim("sub", "actor")], "test") });
        _caller = CommandCaller.From(AuthorizationPrincipalIdentity.Capture(principal));
    }

    [Fact] void should_exclude_actor_claims() => _caller.Single("sub").Key.ShouldEqual("represented");
}
