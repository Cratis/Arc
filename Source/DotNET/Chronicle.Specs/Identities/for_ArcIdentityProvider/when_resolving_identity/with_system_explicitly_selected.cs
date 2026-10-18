// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_system_explicitly_selected : given.an_identity_provider
{
    void Because()
    {
        using var scope = _execution.As(_principal);
        _provider.SetCurrentIdentity(ChronicleIdentity.System);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_explicit_system_identity() => _result.ShouldEqual(ChronicleIdentity.System);
}
