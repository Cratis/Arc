// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_reactor_identity_and_an_override : given.an_identity_provider
{
    readonly ChronicleIdentity _cause = new("trigger", "Trigger", OnBehalfOf: new("origin", "Origin"));
    ChronicleIdentity _reactor;
    ChronicleIdentity _restored;

    void Establish() => _reactor = ChronicleIdentity.System with { OnBehalfOf = _cause };

    void Because()
    {
        _provider.SetCurrentIdentity(_reactor);
        using (_execution.As(_principal))
        {
            _result = _provider.GetCurrent();
        }

        _restored = _provider.GetCurrent();
    }

    [Fact] void should_record_the_override_principal() => _result.ShouldEqual(new ChronicleIdentity("person-123", "Person", "person", _cause));
    [Fact] void should_preserve_the_entire_on_behalf_of_chain() => _result.OnBehalfOf.ShouldBeSame(_cause);
    [Fact] void should_restore_the_reactor_identity_after_the_override() => _restored.ShouldEqual(_reactor);
}
