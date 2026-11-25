// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_not_over_an_audience_that_is_held : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            new AvailableNotAudience("Incident"),
            Requests.For(Requests.Holding("Incident")));

    [Fact] void should_be_unavailable() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
    [Fact] void should_name_the_gate_part() => _result.Causes.ShouldContain("not Incident");
}
