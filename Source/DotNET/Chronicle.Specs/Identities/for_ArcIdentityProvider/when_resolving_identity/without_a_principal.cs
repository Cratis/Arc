// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class without_a_principal : given.an_identity_provider
{
    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_use_system() => _result.ShouldEqual(ChronicleIdentity.System);
}
