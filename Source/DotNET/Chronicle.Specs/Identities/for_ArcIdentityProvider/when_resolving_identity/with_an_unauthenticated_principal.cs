// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_unauthenticated_principal : given.an_identity_provider
{
    void Because()
    {
        using var scope = _execution.As(new ClaimsPrincipal(new ClaimsIdentity(_principal.Claims)));
        _result = _provider.GetCurrent();
    }

    [Fact] void should_not_attribute_unauthenticated_claims() => _result.ShouldEqual(ChronicleIdentity.System);
}
