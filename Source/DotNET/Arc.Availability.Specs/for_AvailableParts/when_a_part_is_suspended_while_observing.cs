// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_AvailableParts;

public class when_a_part_is_suspended_while_observing : given.an_availability_host
{
    readonly List<UnavailablePart[]> _emissions = [];
    IDisposable _subscription;
    IDisposable _subject;

    protected override IEnumerable<Type> Types => [typeof(LooseCommand)];

    async Task Because()
    {
        var subject = AvailableParts.ObserveUnavailable(_scope.ServiceProvider.GetRequiredService<IUnavailableParts>());
        _subject = (IDisposable)subject;
        _subscription = subject.Subscribe(parts => _emissions.Add([.. parts.Unavailable]));
        await WaitFor(1);
        await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(typeof(LooseCommand)));
        await WaitFor(2);
        await _suspensions.Resume(AudienceLevel.Tenant, "acme", PartPaths.For(typeof(LooseCommand)));
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

    [Fact] void should_emit_nothing_unavailable_first() => _emissions[0].ShouldBeEmpty();
    [Fact] void should_emit_the_suspended_part_with_its_reason() => _emissions[1].Single().Reason.ShouldEqual(AvailabilityReason.Suspended);
    [Fact] void should_emit_again_when_it_is_resumed() => _emissions[2].ShouldBeEmpty();

    [Command]
    public record LooseCommand;
}
