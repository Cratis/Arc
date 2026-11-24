// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_not_over_audiences_that_are_not_resolved : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            new AvailableNotAudience("Incident"),
            Requests.For(null));

    [Fact] void should_stay_unknown_because_unknown_never_opens_a_gate() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unknown);
}
