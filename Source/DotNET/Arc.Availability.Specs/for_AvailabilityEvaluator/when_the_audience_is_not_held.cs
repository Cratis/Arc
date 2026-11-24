// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_the_audience_is_not_held : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.For("Plus"),
            Requests.For(Requests.Holding("Preview")));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(false);
    [Fact] void should_report_the_availability_reason() => _result.Reason.ShouldEqual(AvailabilityReasons.NotAvailable);
    [Fact] void should_name_the_gate_part() => _result.Causes.ShouldContain("for Plus");
}
