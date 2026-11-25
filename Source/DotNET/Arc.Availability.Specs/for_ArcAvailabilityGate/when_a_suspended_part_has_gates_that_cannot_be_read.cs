// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_suspended_part_has_gates_that_cannot_be_read : given.a_suspension_host
{
    AvailabilityDecision _decision;

    protected override IAudienceSource AudienceSource
    {
        get
        {
            var source = Substitute.For<IAudienceSource>();
            source.GetGrants(Arg.Any<AudienceSubject>(), Arg.Any<CancellationToken>())
                .Returns<ValueTask<IReadOnlyCollection<AudienceGrant>>>(_ => throw new InvalidOperationException("store is down"));
            return source;
        }
    }

    async Task Establish() => await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(typeof(PlusCommand)));

    async Task Because() => _decision = await Decide(typeof(PlusCommand));

    [Fact] void should_decide_suspended_before_the_gates_are_evaluated() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
}
