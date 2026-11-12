// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_AvailableParts;

public class when_an_audience_is_granted_while_observing : given.an_availability_host
{
    readonly List<string[]> _emissions = [];
    IDisposable _subscription;
    IDisposable _subject;

    async Task Because()
    {
        var subject = AvailableParts.ObserveUnavailable(_scope.ServiceProvider.GetRequiredService<IUnavailableParts>());
        _subject = (IDisposable)subject;
        _subscription = subject.Subscribe(parts => _emissions.Add([.. parts.Unavailable]));
        await WaitFor(1);
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        await WaitFor(2);
        await _audiences.Grant(AudienceLevel.User, "alice", "Plus");
        await _audiences.Withdraw(AudienceLevel.Tenant, "acme", "Plus");
        await _audiences.Withdraw(AudienceLevel.User, "alice", "Plus");
        await WaitFor(3);
    }

    void Destroy()
    {
        _subscription.Dispose();
        _subject.Dispose();
    }

    async Task WaitFor(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_emissions.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    [Fact] void should_emit_what_is_unavailable_first() => _emissions[0].ShouldContain(typeof(given.an_availability_host.PlusCommand).FullName);
    [Fact] void should_emit_again_when_the_grant_opens_a_part() => _emissions[1].ShouldNotContain(typeof(given.an_availability_host.PlusCommand).FullName);
    [Fact] void should_emit_again_when_the_withdrawal_closes_it() => _emissions[2].ShouldContain(typeof(given.an_availability_host.PlusCommand).FullName);
    [Fact] void should_not_emit_when_nothing_changed() => _emissions.Count.ShouldEqual(3);
}
