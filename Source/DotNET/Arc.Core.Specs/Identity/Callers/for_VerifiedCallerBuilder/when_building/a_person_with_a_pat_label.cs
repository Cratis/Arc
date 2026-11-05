// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vector 16.</summary>
public class a_person_with_a_pat_label : given.a_builder
{
    void Establish() => _facts = _facts with { Client = new("privileged-agent-fixture", CallerClientAuthentication.Authenticated) };
    void Because() => Build();

    [Fact] void should_treat_the_label_as_declared() => _result.Caller.Client.Authentication.ShouldEqual(CallerClientAuthentication.Declared);
    [Fact] void should_not_invent_actor_authority() => _result.Caller.Actor.ShouldBeNull();
}
