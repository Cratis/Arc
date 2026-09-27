// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_OperationContextScope;

public class when_a_decorator_enters_sibling_pipelines : Specification
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _validation;
    DateTimeOffset? _execution;
    DateTimeOffset? _query;
    DateTimeOffset? _nested;
    DateTimeOffset? _restored;
    DateTimeOffset? _after;

    async Task Because()
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received)).BuildServiceProvider();
        var nestedServices = new ServiceCollection().AddSingleton<TimeProvider>(new FixedClock(_received.AddMinutes(1))).BuildServiceProvider();
        using (OperationContextScope.Begin(services))
        {
            using var dispatch = OperationContextScope.ForwardTransportReceipt();
            _validation = await EnterCommandValidation();
            _execution = await EnterCommandExecution();
            _query = await EnterQueryPipeline();
            _restored = new OperationContextAccessor().ReceivedAt;
        }
        _after = new OperationContextAccessor().ReceivedAt;

        async Task<DateTimeOffset?> EnterCommandValidation()
        {
            using var entry = OperationContextScope.BeginPipeline(services);
            await Task.Yield();
            return new OperationContextAccessor().ReceivedAt;
        }

        async Task<DateTimeOffset?> EnterCommandExecution()
        {
            using var entry = OperationContextScope.BeginPipeline(services);
            await Task.Yield();
            _nested = await EnterNestedCommand();
            return new OperationContextAccessor().ReceivedAt;
        }

        async Task<DateTimeOffset?> EnterQueryPipeline()
        {
            using var entry = OperationContextScope.BeginPipeline(services);
            await Task.Yield();
            return new OperationContextAccessor().ReceivedAt;
        }

        async Task<DateTimeOffset?> EnterNestedCommand()
        {
            using var entry = OperationContextScope.BeginPipeline(nestedServices);
            await Task.Yield();
            return new OperationContextAccessor().ReceivedAt;
        }
    }

    [Fact] void should_preserve_the_transport_receipt_for_validation_and_execution() =>
        (_validation, _execution).ShouldEqual((_received, _received));
    [Fact] void should_preserve_the_transport_receipt_across_query_and_command_entries() => _query.ShouldEqual(_received);
    [Fact] void should_capture_a_new_receipt_for_a_call_from_inside_a_pipeline_entry() => _nested.ShouldEqual(_received.AddMinutes(1));
    [Fact] void should_restore_the_transport_receipt_after_nested_execution() => _restored.ShouldEqual(_received);
    [Fact] void should_clear_the_receipt_after_dispatch() => _after.ShouldBeNull();

    sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
