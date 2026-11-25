// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_with_no_gate_is_suspended_and_resumed : given.a_suspension_host
{
    AvailabilityDecision _before;
    AvailabilityDecision _suspended;
    AvailabilityDecision _resumed;

    async Task Because()
    {
        _before = await Decide(Ungated);
        await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(Ungated));
        ResetRequest();
        _suspended = await Decide(Ungated);
        await _suspensions.Resume(AudienceLevel.Tenant, "acme", PartPaths.For(Ungated));
        ResetRequest();
        _resumed = await Decide(Ungated);
    }

    void ResetRequest() => _scope.ServiceProvider.GetRequiredService<AvailabilityRequestContext>().Reset();

    [Fact] void should_be_available_before() => _before.IsAvailable.ShouldBeTrue();
    [Fact] void should_be_suspended() => _suspended.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
    [Fact] void should_report_the_suspended_reason() => _suspended.Reason.ShouldEqual(AvailabilityReason.Suspended);
    [Fact] void should_not_report_it_as_not_available() => _suspended.Reason.ShouldNotEqual(AvailabilityReason.NotAvailable);
    [Fact] void should_be_available_again_when_resumed() => _resumed.IsAvailable.ShouldBeTrue();
}
