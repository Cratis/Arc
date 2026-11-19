// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_condition_reads_a_value_that_is_absent : given.an_availability_host
{
    AvailabilityDecision _decision;

    protected override void Configure(ArcAvailabilityOptions options) =>
        options.Gate<given.an_availability_host.UngatedCommand>(AvailabilityExpression.When(new ContextComparison("vessel.commercialPackage", ComparisonOperator.NotEqual, "Basic")));

    async Task Because() => _decision = await _gate.Decide(typeof(given.an_availability_host.UngatedCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_be_unknown() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_not_be_available() => _decision.IsAvailable.ShouldBeFalse();
}
