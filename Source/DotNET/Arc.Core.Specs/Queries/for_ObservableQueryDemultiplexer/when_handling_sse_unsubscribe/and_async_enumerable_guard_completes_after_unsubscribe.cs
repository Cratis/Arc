// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

/// <summary>
/// An async-enumerable guard can still be awaiting when its subscription is explicitly removed. Its eventual verdict
/// must observe the subscription cancellation before producing an authorization outcome or writing the emission.
/// </summary>
public class and_async_enumerable_guard_completes_after_unsubscribe : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _guardRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _guardStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _streamEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _streamingData = Stream();

    async Task Because() => await RunConnection(async () =>
    {
        await _guardStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Unsubscribe(FirstQueryId);

        _guardRelease.TrySetResult();
        await _streamEnded.Task.WaitAsync(TimeSpan.FromSeconds(2));
    });

    [Fact] void should_have_reached_the_awaiting_guard() => _guardStarted.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_send_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_track_data_as_served() => _healthTracker.DidNotReceive().RecordDataServed(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_only_unregister_the_explicitly_unsubscribed_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(new AwaitingGuard(_guardStarted, _guardRelease));
        guardTypes.Add(typeof(AwaitingGuard));
    }

    async IAsyncEnumerable<IEnumerable<string>> Stream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            yield return ["item-a"];
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            _streamEnded.TrySetResult();
        }
    }

    public class AwaitingGuard(TaskCompletionSource started, TaskCompletionSource release) : IGuardObservableQueryEmission
    {
        public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            started.TrySetResult();
            await release.Task;
            return ObservableQueryEmissionVerdict.DenyAndTerminate;
        }
    }
}
