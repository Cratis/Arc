// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_disposing;

public class with_a_resource_that_disposes_asynchronously : Specification
{
    StreamingQuerySubscription _subscription;
    PendingResource _first;
    IDisposable _second;
    bool _secondDisposedWhileGated;
    bool _completedWhileGated;

    void Establish()
    {
        _subscription = new(CancellationToken.None);
        _first = new();
        _second = Substitute.For<IDisposable>();
        _subscription.AddResource(_first);
        _subscription.AddResource(_second);
    }

    async Task Because()
    {
        _subscription.Dispose();
        _subscription.Dispose();
        _secondDisposedWhileGated = _second.ReceivedCalls().Any();
        _completedWhileGated = _subscription.ResourcesDisposed.IsCompleted;
        _first.Release.TrySetResult();
        await _subscription.ResourcesDisposed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact] void should_not_dispose_the_second_resource_while_the_first_is_pending() => _secondDisposedWhileGated.ShouldBeFalse();
    [Fact] void should_not_complete_teardown_while_the_first_is_pending() => _completedWhileGated.ShouldBeFalse();
    [Fact] void should_dispose_the_second_resource_once() => _second.Received(1).Dispose();
    [Fact] void should_dispose_the_first_resource_asynchronously_once() => _first.AsyncDisposals.ShouldEqual(1);
    [Fact] void should_not_dispose_the_first_resource_synchronously() => _first.SyncDisposals.ShouldEqual(0);

    sealed class PendingResource : IDisposable, IAsyncDisposable
    {
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int AsyncDisposals { get; private set; }
        public int SyncDisposals { get; private set; }

        public async ValueTask DisposeAsync()
        {
            AsyncDisposals++;
            await Release.Task;
        }

        public void Dispose()
        {
            SyncDisposals++;
            throw new InvalidOperationException("Synchronous disposal must not be used");
        }
    }
}
