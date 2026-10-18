// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_reactor_identity_and_an_unauthenticated_override : given.an_identity_provider
{
    readonly ChronicleIdentity _reactor = ChronicleIdentity.System with { OnBehalfOf = new("trigger", "Trigger") };

    void Because()
    {
        _provider.SetCurrentIdentity(_reactor);
        using var scope = _execution.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "anonymous")])));
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_reactor_identity() => _result.ShouldEqual(_reactor);
}
