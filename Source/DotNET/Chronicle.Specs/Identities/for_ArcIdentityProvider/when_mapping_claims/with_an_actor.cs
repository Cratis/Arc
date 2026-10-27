// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_mapping_claims;

public class with_an_actor : given.an_on_behalf_principal
{
    ChronicleIdentity _mapped;

    void Because() => _mapped = ArcIdentityProvider.FromPrincipal(_onBehalfPrincipal);

    [Fact] void should_put_the_actor_first() => _mapped.ShouldEqual(_actor with { OnBehalfOf = _represented });
    [Fact] void should_act_on_behalf_of_the_represented_user() => _mapped.OnBehalfOf.ShouldEqual(_represented);
    [Fact] void should_end_the_chain_with_the_represented_user() => _mapped.OnBehalfOf!.OnBehalfOf.ShouldBeNull();
}
