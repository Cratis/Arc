// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_the_audience_is_held : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.For("Plus"),
            Requests.For(Requests.Holding("Plus")));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Available);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(true);
    [Fact] void should_carry_no_reason() => _result.Reason.ShouldBeNull();
}
