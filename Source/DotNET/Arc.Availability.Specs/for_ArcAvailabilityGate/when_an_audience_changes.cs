// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_an_audience_changes : given.an_availability_host
{
    int _announced;

    async Task Because()
    {
        using var subscription = _gate.OnChanged(() =>
        {
            _announced++;
            return ValueTask.CompletedTask;
        });
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        await _audiences.Withdraw(AudienceLevel.Tenant, "acme", "Plus");
    }

    [Fact] void should_announce_every_change() => _announced.ShouldEqual(2);
}
