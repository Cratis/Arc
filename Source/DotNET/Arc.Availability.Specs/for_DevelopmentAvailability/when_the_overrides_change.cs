// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_the_overrides_change : given.a_development_host
{
    int _announced;
    int _announcedAfterDisposal;

    async Task Because()
    {
        var subscription = _gate.OnChanged(() =>
        {
            _announced++;
            return ValueTask.CompletedTask;
        });
        await _development.Apply(AvailabilityOverrides.None with { EnabledAudiences = ["Plus"] });
        subscription.Dispose();
        await _development.Apply(AvailabilityOverrides.None);
        _announcedAfterDisposal = _announced;
    }

    [Fact] void should_announce_the_change_so_open_sessions_decide_again() => _announced.ShouldEqual(1);
    [Fact] void should_stop_announcing_once_the_subscription_ends() => _announcedAfterDisposal.ShouldEqual(1);
}
