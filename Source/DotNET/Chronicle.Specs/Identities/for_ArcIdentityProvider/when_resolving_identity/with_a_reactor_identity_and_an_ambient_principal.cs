// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_reactor_identity_and_an_ambient_principal : given.an_identity_provider
{
    readonly ChronicleIdentity _reactor = ChronicleIdentity.System with { OnBehalfOf = new("trigger", "Trigger") };

    void Establish()
    {
        _principals = Substitute.For<ICurrentPrincipalAccessor>();
        _principals.Current.Returns(_principal);
        _provider = new(_fallback, _principals, () => false);
    }

    void Because()
    {
        _provider.SetCurrentIdentity(_reactor);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_reactor_identity_without_an_override() => _result.ShouldEqual(_reactor);
}
