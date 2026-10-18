// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_system_explicitly_selected_inside_an_override : given.an_identity_provider
{
    void Because()
    {
        _provider.SetCurrentIdentity(ChronicleIdentity.System);
        using var scope = _execution.As(_principal);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_record_the_override_principal() => _result.ShouldEqual(new ChronicleIdentity("person-123", "Person", "person"));
}
