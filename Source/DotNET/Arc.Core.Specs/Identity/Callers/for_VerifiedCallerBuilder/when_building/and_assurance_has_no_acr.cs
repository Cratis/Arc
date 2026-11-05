// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class and_assurance_has_no_acr : given.a_builder
{
    void Establish() => _facts = _facts with { Assurance = _facts.Assurance! with { Acr = null } };
    void Because() => Build();

    [Fact] void should_default_only_the_context_to_basic() => _result.Caller.Assurance.Acr.ShouldEqual("basic");
    [Fact] void should_not_invent_an_authentication_time() => _result.Caller.Assurance.AuthTime.ShouldEqual(_facts.Assurance.AuthTime);
}
