// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_a_part_that_declares_no_gate_is_switched_off : given.a_development_host
{
    bool _gatedBeforehand;
    bool _gated;
    IReadOnlyCollection<Type> _gatedParts;
    AvailabilityDecision _decision;

    async Task Because()
    {
        _gatedBeforehand = _gate.IsGated(typeof(UngatedCommand));
        await _development.Apply(AvailabilityOverrides.None with { DisabledPaths = [PathOf(typeof(UngatedCommand))] });
        _gated = _gate.IsGated(typeof(UngatedCommand));
        _gatedParts = _gate.GatedParts;
        _decision = await Decide(typeof(UngatedCommand));
    }

    [Fact] void should_not_be_gated_beforehand() => _gatedBeforehand.ShouldBeFalse();
    [Fact] void should_gate_the_part() => _gated.ShouldBeTrue();
    [Fact] void should_report_the_part_among_the_gated_parts() => _gatedParts.ShouldContain(typeof(UngatedCommand));
    [Fact] void should_not_make_the_part_available() => _decision.IsAvailable.ShouldBeFalse();
}
