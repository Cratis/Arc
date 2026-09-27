// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_a_decorated_dispatch_forks_work : Specification
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset?[] _siblings = [];
    DateTimeOffset? _late;

    async Task Because()
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received)).BuildServiceProvider();
        var laterServices = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received.AddMinutes(1))).BuildServiceProvider();
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        Task<DateTimeOffset?> late;
        using (OperationContextScope.Begin(services))
        {
            using var dispatch = OperationContextScope.ForwardTransportReceipt();
            var first = Task.Run(EnterSibling);
            var second = Task.Run(EnterSibling);
            late = Task.Run(async () =>
            {
                await dispatchFinished.Task;
                using var entry = OperationContextScope.BeginPipeline(laterServices);
                return new OperationContextAccessor().ReceivedAt;
            });
            _siblings = await Task.WhenAll(first, second);
        }
        dispatchFinished.SetResult();
        _late = await late;

        async Task<DateTimeOffset?> EnterSibling()
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                bothStarted.SetResult();
            }
            await bothStarted.Task;
            using var entry = OperationContextScope.BeginPipeline(services);
            await Task.Yield();
            return new OperationContextAccessor().ReceivedAt;
        }
    }

    [Fact] void should_forward_the_transport_receipt_to_both_concurrent_siblings()
    {
        _siblings.Length.ShouldEqual(2);
        _siblings[0].ShouldEqual(_received);
        _siblings[1].ShouldEqual(_received);
    }
    [Fact] void should_capture_a_fresh_receipt_for_work_started_after_dispatch() => _late.ShouldEqual(_received.AddMinutes(1));

    sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
