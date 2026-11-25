// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_operations_have_the_same_receipt : Specification
{
    object _outer;
    object _nested;
    object _parallel;
    object _restored;
    object _after;
    DateTimeOffset? _firstReceipt;
    DateTimeOffset? _secondReceipt;

    async Task Because()
    {
        await using var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock()).BuildServiceProvider();
        using (OperationContextScope.Begin(services))
        {
            _outer = OperationContextScope.Identity;
            _firstReceipt = new OperationContextAccessor().ReceivedAt;
            using (OperationContextScope.Begin(services))
            {
                _nested = OperationContextScope.Identity;
                _secondReceipt = new OperationContextAccessor().ReceivedAt;
            }

            _parallel = await Task.Run(() =>
            {
                using var scope = OperationContextScope.Begin(services);
                return OperationContextScope.Identity;
            });
            _restored = OperationContextScope.Identity;
        }
        _after = OperationContextScope.Identity;
    }

    [Fact] void should_keep_the_pinned_receipts_equal() => _firstReceipt.ShouldEqual(_secondReceipt);
    [Fact] void should_not_use_the_receipt_as_nested_identity() => ReferenceEquals(_outer, _nested).ShouldBeFalse();
    [Fact] void should_not_use_the_receipt_as_parallel_identity() => ReferenceEquals(_outer, _parallel).ShouldBeFalse();
    [Fact] void should_restore_the_outer_identity() => _restored.ShouldBeSame(_outer);
    [Fact] void should_clear_identity_after_disposal() => _after.ShouldBeNull();

    sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    }
}
