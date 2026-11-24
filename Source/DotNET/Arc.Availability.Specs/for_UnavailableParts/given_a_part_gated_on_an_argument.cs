// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class given_a_part_gated_on_an_argument : given.an_availability_host
{
    protected override IEnumerable<Type> Types => [typeof(UngatedCommand)];

    protected override void Configure(ArcAvailabilityOptions options) =>
        options.Gate<UngatedCommand>(AvailabilityExpression.When(new ContextComparison("arguments.vesselId", ComparisonOperator.Equal, "allowed")));
}
