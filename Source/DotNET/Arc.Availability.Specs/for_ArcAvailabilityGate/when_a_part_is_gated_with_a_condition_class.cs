// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_is_gated_with_a_condition_class : given.a_typed_availability_host
{
    AvailabilityDecision _decision;

    async Task Because() => _decision = await _gate.Decide(typeof(given.TypedDeclarations.OpaqueCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_ask_the_condition() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Available);
}
