// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_user_holds_the_audience : given.an_availability_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _audiences.Grant(AudienceLevel.User, "alice", "Plus");

    async Task Because() => _decision = await _gate.Decide(typeof(given.an_availability_host.PlusCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_be_available() => _decision.IsAvailable.ShouldBeTrue();
}
