// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_guard_denies_an_async_enumerable_emission : given.a_guarded_websocket_connection
{
    readonly TaskCompletionSource _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _streamEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _streamingData = Stream();
        _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _releaseFirst.TrySetResult();
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId));
        await WaitFor(() => Volatile.Read(ref _unregisteredCount) == 1);
        await _streamEnded.Task.WaitAsync(TimeSpan.FromSeconds(2));
    });

    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_stop_consulting_the_guard() => _guardCalls.Count.ShouldEqual(1);

    async IAsyncEnumerable<IEnumerable<string>> Stream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            await _releaseFirst.Task;
            yield return ["item-a"];
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            _streamEnded.TrySetResult();
        }
    }
}
