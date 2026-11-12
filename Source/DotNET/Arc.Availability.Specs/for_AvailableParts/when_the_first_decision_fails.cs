// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_AvailableParts;

public class when_the_first_decision_fails : given.an_availability_host
{
    Exception _error;
    TaskCompletionSource<Exception> _failed = new();
    IDisposable _subject;

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

    async Task Because()
    {
        var subject = AvailableParts.ObserveUnavailable(_scope.ServiceProvider.GetRequiredService<IUnavailableParts>());
        _subject = (IDisposable)subject;
        using var subscription = subject.Subscribe(_ => { }, error => _failed.TrySetResult(error));
        _error = await _failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _subject.Dispose();

    [Fact] void should_fail_the_query_as_not_decided() => _error.ShouldBeOfExactType<AvailabilityNotDecidedException>();
}
