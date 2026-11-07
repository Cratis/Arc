// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_exiting_the_last_operation;

public class and_producer_detachment_is_pending : given.a_subscription_with_a_blocked_stop
{
    bool _resourceDisposedWhileStopping;
    bool _scopeDisposedWhileStopping;
    bool _completedWhileStopping;
    bool _cancelledWhileStopping;

    void Establish() => _producer.When(producer => producer.Dispose()).Do(_ => PauseStopping());

    async Task Because()
    {
        var disposal = Task.Run(_subscription.Dispose);
        try
        {
            await _stopStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            _subscription.Exit();
            _resourceDisposedWhileStopping = _resource.ReceivedCalls().Any();
            _scopeDisposedWhileStopping = _scope.ReceivedCalls().Any();
            _completedWhileStopping = _subscription.ResourcesDisposed.IsCompleted;
            _cancelledWhileStopping = _subscription.Token.IsCancellationRequested;
        }
        finally
        {
            _releaseStop.TrySetResult();
            await disposal.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }

        await _subscription.ResourcesDisposed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        _subscription.Dispose();
    }

    [Fact] void should_keep_the_resource_until_the_producer_is_detached() => _resourceDisposedWhileStopping.ShouldBeFalse();
    [Fact] void should_keep_the_scope_until_the_producer_is_detached() => _scopeDisposedWhileStopping.ShouldBeFalse();
    [Fact] void should_not_complete_teardown_until_the_producer_is_detached() => _completedWhileStopping.ShouldBeFalse();
    [Fact] void should_cancel_before_detaching_the_producer() => _cancelledWhileStopping.ShouldBeTrue();
    [Fact] void should_detach_the_producer_once() => _producer.Received(1).Dispose();
    [Fact] void should_dispose_the_resource_once() => _resource.Received(1).Dispose();
    [Fact] void should_dispose_the_scope_once() => _scope.Received(1).Dispose();
}
