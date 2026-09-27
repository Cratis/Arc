// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_entering_a_pipeline_without_a_decorator : Specification
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _pipeline;
    DateTimeOffset? _hosted;
    DateTimeOffset? _transport;
    DateTimeOffset? _after;

    async Task Because()
    {
        var transportServices = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received)).BuildServiceProvider();
        var pipelineServices = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received.AddMinutes(1))).BuildServiceProvider();
        using (OperationContextScope.Begin(transportServices))
        {
            using (OperationContextScope.BeginIfNotSet(pipelineServices))
            {
                _hosted = new OperationContextAccessor().ReceivedAt;
            }
            using (OperationContextScope.BeginPipeline(pipelineServices))
            {
                await Task.Yield();
                _pipeline = new OperationContextAccessor().ReceivedAt;
            }
            _transport = new OperationContextAccessor().ReceivedAt;
        }
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_preserve_the_transport_receipt_at_the_builtin_hosted_entry() => _hosted.ShouldEqual(_received);
    [Fact] void should_capture_the_direct_pipeline_entry_receipt() => _pipeline.ShouldEqual(_received.AddMinutes(1));
    [Fact] void should_restore_the_prior_context() => _transport.ShouldEqual(_received);
    [Fact] void should_clear_the_context_after_dispatch() => _after.ShouldBeNull();

    sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
