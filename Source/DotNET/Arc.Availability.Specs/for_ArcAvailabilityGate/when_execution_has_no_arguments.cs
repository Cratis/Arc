// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_execution_has_no_arguments : given.an_invocation_host
{
    AvailabilityDecision _missing;
    AvailabilityDecision _null;
    AvailabilityDecision _later;

    async Task Because()
    {
        _missing = await Decide(new Dictionary<string, object>());
        _null = await Decide(null);
        _later = await Decide(new { VesselId = "allowed" });
    }

    [Fact] void should_keep_a_missing_argument_unknown() => _missing.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_keep_a_null_instance_unknown() => _null.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_resolve_again_when_arguments_arrive() => _later.IsAvailable.ShouldBeTrue();
}
