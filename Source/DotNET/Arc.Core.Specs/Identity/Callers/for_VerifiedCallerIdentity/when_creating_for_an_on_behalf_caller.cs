// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerIdentity;

public class when_creating_for_an_on_behalf_caller : Specification
{
    VerifiedCallerIdentity _identity;

    void Because() => _identity = new VerifiedCallerIdentity(verified_callers.Person("person-1") with { Actor = new("https://issuer.example", "actor-1") });

    [Fact] void should_carry_the_actor_subject() => _identity.Actor!.FindFirst("sub")!.Value.ShouldEqual("actor-1");

    [Fact] void should_carry_the_actor_issuer() => _identity.Actor!.FindFirst("iss")!.Value.ShouldEqual("https://issuer.example");
}
