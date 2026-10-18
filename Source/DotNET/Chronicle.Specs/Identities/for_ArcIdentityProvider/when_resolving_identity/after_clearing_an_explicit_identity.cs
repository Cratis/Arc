// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class after_clearing_an_explicit_identity : given.an_identity_provider
{
    void Because()
    {
        using var scope = _execution.As(_principal);
        _provider.SetCurrentIdentity(ChronicleIdentity.System);
        _provider.ClearCurrentIdentity();
        _result = _provider.GetCurrent();
    }

    [Fact] void should_resume_principal_resolution() => _result.ShouldEqual(new ChronicleIdentity("person-123", "Person", "person"));
    [Fact] void should_clear_the_wrapped_provider_identity() => _fallback.GetCurrent().ShouldEqual(ChronicleIdentity.System);
}
