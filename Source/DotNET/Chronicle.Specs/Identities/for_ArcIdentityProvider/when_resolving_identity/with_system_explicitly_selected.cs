// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_system_explicitly_selected : given.an_identity_provider
{
    void Establish()
    {
        _principals = Substitute.For<ICurrentPrincipalAccessor>();
        _principals.Current.Returns(_principal);
        _provider = new(_fallback, _principals, () => false);
    }

    void Because()
    {
        _provider.SetCurrentIdentity(ChronicleIdentity.System);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_explicit_system_identity() => _result.ShouldEqual(ChronicleIdentity.System);
}
