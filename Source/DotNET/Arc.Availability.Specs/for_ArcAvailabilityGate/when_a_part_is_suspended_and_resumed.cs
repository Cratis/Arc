// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_is_suspended_and_resumed : given.a_suspension_host
{
    int _announced;

    async Task Because()
    {
        using var subscription = _gate.OnChanged(() =>
        {
            _announced++;
            return ValueTask.CompletedTask;
        });
        await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(Ungated));
        await _suspensions.Resume(AudienceLevel.Tenant, "acme", PartPaths.For(Ungated));
    }

    [Fact] void should_announce_every_change_so_open_sessions_decide_again() => _announced.ShouldEqual(2);
}
