// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ContextComparison;

public class when_the_value_is_greater : Specification
{
    AvailabilityOutcome _outcome;

    async Task Because() =>
        _outcome = await new ContextComparison("v", ComparisonOperator.GreaterThan, 3).Evaluate(
            Requests.For(null, ambient: new Dictionary<string, object?> { ["v"] = 5 }),
            CancellationToken.None);

    [Fact] void should_have_the_outcome() => _outcome.ShouldEqual(AvailabilityOutcome.Available);
}
