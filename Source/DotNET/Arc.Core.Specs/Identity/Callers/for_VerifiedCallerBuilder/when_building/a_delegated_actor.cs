// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vector 1.</summary>
public class a_delegated_actor : given.a_builder
{
    void Establish() => _facts = verified_facts.Delegated();
    void Because() => Build();

    [Fact] void should_retain_the_current_actor() => _result.Caller.Actor.ShouldEqual(_facts.Actor);
    [Fact] void should_retain_the_checked_grant() => _result.Caller.GrantId.ShouldEqual(_facts.Grant.Id);
    [Fact] void should_identify_the_delegation() => _result.Caller.PrincipalKind.ShouldEqual(CallerPrincipalKind.DelegatedActor);
}
