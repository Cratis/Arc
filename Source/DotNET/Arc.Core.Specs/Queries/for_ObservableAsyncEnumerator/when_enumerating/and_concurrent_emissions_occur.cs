// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_enumerating;

public class and_concurrent_emissions_occur
{
    [Fact]
    public async Task should_not_lose_items_when_emitted_rapidly()
    {
        // Arrange
        var subject = new Subject<int>();
        var enumerator = new ObservableAsyncEnumerator<int>(subject, CancellationToken.None);
        var collectedItems = new List<int>();

        // Act: Emit items rapidly to create race conditions
        var emitTask = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                subject.OnNext(i);
                await Task.Yield();
            }

            subject.OnCompleted();
        });

        var consumeTask = Task.Run(async () =>
        {
            while (await enumerator.MoveNextAsync())
            {
                collectedItems.Add(enumerator.Current);
            }
        });

        await Task.WhenAll(emitTask, consumeTask);
        await enumerator.DisposeAsync();

        // Assert
        collectedItems.Count.ShouldEqual(50);
    }

    [Fact]
    public async Task should_handle_disposal_during_pending_move_next()
    {
        // Arrange
        var subject = new Subject<int>();
        var enumerator = new ObservableAsyncEnumerator<int>(subject, CancellationToken.None);

        // Act: Start a MoveNextAsync that will wait for an item
        var moveNextTask = enumerator.MoveNextAsync().AsTask();

        // Give time for the task to enter the wait
        await Task.Delay(100);

        // Dispose while waiting
        var disposeTask = enumerator.DisposeAsync().AsTask();

        // Complete the emission after a brief delay
        subject.OnNext(42);

        // This should complete without deadlock
        var timeout = Task.Delay(2000);
        var firstCompleted = await Task.WhenAny(moveNextTask, disposeTask, timeout);

        // Assert: Should not timeout
        (firstCompleted == timeout).ShouldBeFalse();
    }
}
