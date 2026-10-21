// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_an_authenticated_accessor_principal : given.an_identity_provider
{
    void Establish()
    {
        _principals = Substitute.For<ICurrentPrincipalAccessor>();
        _principals.Current.Returns(_principal);
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because() => _result = _provider.GetCurrent();

    [Fact] void should_map_the_authenticated_principal_without_requiring_a_scope() => _result.ShouldEqual(new ChronicleIdentity("person-123", "Person", "person"));
}
