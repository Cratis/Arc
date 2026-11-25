// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_suspension_is_for_the_user : given.a_suspension_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _suspensions.Suspend(AudienceLevel.User, "alice", PartPaths.For(Ungated));

    async Task Because() => _decision = await Decide(Ungated);

    [Fact] void should_suspend_the_part() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
}
