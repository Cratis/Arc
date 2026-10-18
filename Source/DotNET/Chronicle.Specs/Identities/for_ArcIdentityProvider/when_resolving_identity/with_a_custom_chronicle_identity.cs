// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_resolving_identity;

public class with_a_custom_chronicle_identity : given.an_identity_provider
{
    void Establish()
    {
        _fallback = Substitute.For<IIdentityProvider>();
        _fallback.GetCurrent().Returns(new ChronicleIdentity("custom", "Custom identity"));
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
    }

    void Because()
    {
        using var scope = _execution.As(_principal);
        _result = _provider.GetCurrent();
    }

    [Fact] void should_preserve_the_selected_providers_identity() => _result.ShouldEqual(new ChronicleIdentity("custom", "Custom identity"));
}
