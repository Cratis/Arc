// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_CompositeCondition;

public class when_or_has_an_unknown_and_an_available_part : Specification
{
    AvailabilityOutcome _outcome;

    async Task Because() =>
        _outcome = await new CompositeCondition(false, new FixedCondition(AvailabilityOutcome.Unknown), new FixedCondition(AvailabilityOutcome.Available))
            .Evaluate(Requests.For(null), CancellationToken.None);

    [Fact] void should_be_available() => _outcome.ShouldEqual(AvailabilityOutcome.Available);
}
