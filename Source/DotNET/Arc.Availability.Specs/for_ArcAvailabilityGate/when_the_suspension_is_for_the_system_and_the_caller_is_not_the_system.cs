// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_suspension_is_for_the_system_and_the_caller_is_not_the_system : given.a_suspension_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _suspensions.Suspend(AudienceLevel.System, null, PartPaths.For(Ungated));

    async Task Because() => _decision = await Decide(Ungated);

    [Fact] void should_not_suspend_the_part_for_the_caller() => _decision.IsAvailable.ShouldBeTrue();
}
