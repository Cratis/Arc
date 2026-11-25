// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_evaluating_a_path_of_gates : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).EvaluatePath(
            [AvailabilityExpression.For("Plus"), AvailabilityExpression.For("Preview")],
            Requests.For(Requests.Holding("Plus")));

    [Fact] void should_deny_when_any_gate_denies() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
    [Fact] void should_name_the_denying_gate() => _result.Causes.ShouldContain("for Preview");
}
