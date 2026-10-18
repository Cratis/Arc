// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_explicit_identity : given.an_identity_provider
{
    readonly ChronicleIdentity _explicit = new("reactor", "Reactor", OnBehalfOf: new("person", "Person"));
    ChronicleIdentity _wrapped;

    void Because()
    {
        using var scope = _execution.As(_principal);
        _provider.SetCurrentIdentity(_explicit);
        _result = _provider.GetCurrent();
        _wrapped = _fallback.GetCurrent();
    }

    [Fact] void should_preserve_the_explicit_identity_chain() => _result.ShouldEqual(_explicit);
    [Fact] void should_set_the_wrapped_provider_identity() => _wrapped.ShouldEqual(_explicit);
}
