// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_system_execution_scope : given.an_identity_provider
{
    ChronicleIdentity _afterScope;

    void Because()
    {
        using (_execution.As(_principal))
        {
            _result = _provider.GetCurrent();
        }
        _afterScope = _provider.GetCurrent();
    }

    [Fact] void should_record_the_executing_principal() => _result.ShouldEqual(new ChronicleIdentity("person-123", "Person", "person"));
    [Fact] void should_restore_system_after_the_scope() => _afterScope.ShouldEqual(ChronicleIdentity.System);
}
