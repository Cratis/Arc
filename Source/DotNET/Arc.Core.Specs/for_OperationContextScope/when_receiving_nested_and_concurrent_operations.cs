// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_receiving_nested_and_concurrent_operations : Specification
{
    static readonly DateTimeOffset _first = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    static readonly DateTimeOffset _second = _first.AddHours(1);
    readonly IOperationContextAccessor _accessor = new OperationContextAccessor();
    DateTimeOffset? _outer;
    DateTimeOffset? _inner;
    DateTimeOffset? _restored;
    DateTimeOffset? _concurrent;
    DateTimeOffset? _after;

    async Task Because()
    {
        var firstProvider = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_first)).BuildServiceProvider();
        var secondProvider = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_second)).BuildServiceProvider();
        using (OperationContextScope.Begin(firstProvider))
        {
            _outer = _accessor.ReceivedAt;
            await Task.Yield();
            using (OperationContextScope.Begin(secondProvider))
            {
                _inner = _accessor.ReceivedAt;
            }
            _restored = _accessor.ReceivedAt;
            _concurrent = await Task.Run(async () =>
            {
                using var other = OperationContextScope.Begin(secondProvider);
                await Task.Yield();
                return _accessor.ReceivedAt;
            });
            _restored = _accessor.ReceivedAt;
        }
        _after = _accessor.ReceivedAt;
    }

    [Fact] void should_capture_the_configured_clock() => _outer.ShouldEqual(_first);
    [Fact] void should_give_the_nested_operation_its_own_receipt() => _inner.ShouldEqual(_second);
    [Fact] void should_restore_the_outer_operation_across_awaits() => _restored.ShouldEqual(_first);
    [Fact] void should_isolate_concurrent_operations() => _concurrent.ShouldEqual(_second);
    [Fact] void should_clear_the_accessor_after_the_operation() => _after.ShouldBeNull();

    sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
