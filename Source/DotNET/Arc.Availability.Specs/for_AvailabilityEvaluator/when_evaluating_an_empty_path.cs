// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_evaluating_an_empty_path : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).EvaluatePath([], Requests.For(Requests.Holding()));

    [Fact] void should_be_available() => _result.IsAvailable.ShouldBeTrue();
}
