// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_arguments_claim_an_audience : given.an_invocation_host
{
    AvailabilityDecision _decision;

    protected override void Configure(ArcAvailabilityOptions options) => options.Gate<UngatedCommand>(AvailabilityExpression.For("Plus"));

    async Task Because() => _decision = await Decide(new Dictionary<string, object>
    {
        ["audiences"] = new[] { "Plus" },
        ["tenant"] = "privileged",
        ["identity"] = new { User = "administrator", IsSystem = true }
    });

    [Fact] void should_not_grant_the_claimed_audience() => _decision.IsAvailable.ShouldBeFalse();
}
