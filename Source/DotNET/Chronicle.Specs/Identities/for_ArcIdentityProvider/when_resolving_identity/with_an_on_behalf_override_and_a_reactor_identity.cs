// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_on_behalf_override_and_a_reactor_identity : given.an_on_behalf_principal
{
    readonly ChronicleIdentity _cause = new("trigger", "Trigger");

    void Because()
    {
        _provider.SetCurrentIdentity(ChronicleIdentity.System with { OnBehalfOf = _cause });
        using var scope = _execution.As(_onBehalfPrincipal);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_keep_the_actor_first_then_the_represented_user_then_the_cause() =>
        _result.ShouldEqual(_actor with { OnBehalfOf = _represented with { OnBehalfOf = _cause } });
}
