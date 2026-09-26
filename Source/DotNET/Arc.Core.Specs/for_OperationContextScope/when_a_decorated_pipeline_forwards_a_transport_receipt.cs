// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_a_decorated_pipeline_forwards_a_transport_receipt : Specification
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _forwarded;
    DateTimeOffset? _nested;
    DateTimeOffset? _restored;
    DateTimeOffset? _after;

    async Task Because()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received, _received.AddMinutes(1));
        var services = new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider();
        using (OperationContextScope.Begin(services))
        {
            using var forwardedReceipt = OperationContextScope.ForwardTransportReceipt();
            await EnterPublicPipeline();
            await EnterPublicPipeline();
            _restored = new OperationContextAccessor().ReceivedAt;
        }
        _after = new OperationContextAccessor().ReceivedAt;

        async Task EnterPublicPipeline()
        {
            using var receipt = OperationContextScope.BeginPipeline(services);
            await Task.Yield();
            if (_forwarded is null)
            {
                _forwarded = new OperationContextAccessor().ReceivedAt;
            }
            else
            {
                _nested = new OperationContextAccessor().ReceivedAt;
            }
        }
    }

    [Fact] void should_preserve_the_receipt_through_the_forwarded_entry() => _forwarded.ShouldEqual(_received);
    [Fact] void should_capture_a_new_receipt_for_a_nested_call() => _nested.ShouldEqual(_received.AddMinutes(1));
    [Fact] void should_restore_the_transport_receipt_after_nested_execution() => _restored.ShouldEqual(_received);
    [Fact] void should_clear_the_receipt_after_dispatch() => _after.ShouldBeNull();
}
