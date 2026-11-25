// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_arguments_try_to_replace_trusted_context : given.an_invocation_host
{
    AvailabilityDecision _decision;

    protected override void Configure(ArcAvailabilityOptions options) => options.Gate<UngatedCommand>(AvailabilityExpression.AllOf(
        AvailabilityExpression.When(new ContextComparison("tenant", ComparisonOperator.Equal, "acme")),
        AvailabilityExpression.When(new ContextComparison("identity.user", ComparisonOperator.Equal, "alice")),
        AvailabilityExpression.When(new ContextComparison("receivedAt", ComparisonOperator.Equal, Receipt)),
        AvailabilityExpression.When(new ContextComparison("arguments.vesselId", ComparisonOperator.Equal, "allowed"))));

    async Task Because() => _decision = await Decide(new Dictionary<string, object>
    {
        ["tenant"] = "attacker", ["identity"] = new { User = "attacker" }, ["receivedAt"] = DateTimeOffset.MinValue,
        ["audiences"] = new[] { "Plus" }, ["$context"] = new { Selected = "allowed" }, ["vesselId"] = "allowed"
    });

    [Fact] void should_keep_the_server_subject_and_receipt() => _decision.IsAvailable.ShouldBeTrue();
}
