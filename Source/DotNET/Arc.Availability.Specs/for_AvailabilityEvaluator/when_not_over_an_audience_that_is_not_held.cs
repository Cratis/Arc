// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_not_over_an_audience_that_is_not_held : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            new AvailableNotAudience("Incident"),
            Requests.For(Requests.Holding("Plus")));

    [Fact] void should_be_available() => _result.IsAvailable.ShouldBeTrue();
}
