// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_policy_and_organization_context : given.a_builder
{
    void Establish() => _facts = _facts with { Organisation = new("organization-fixture"), PolicyEpoch = 12 };
    void Because() => Build();

    [Fact] void should_retain_the_verified_organization() => _result.Caller.Organisation.ShouldEqual(_facts.Organisation);
    [Fact] void should_retain_the_verified_policy_epoch() => _result.Caller.PolicyEpoch.ShouldEqual((ulong?)12);
}
