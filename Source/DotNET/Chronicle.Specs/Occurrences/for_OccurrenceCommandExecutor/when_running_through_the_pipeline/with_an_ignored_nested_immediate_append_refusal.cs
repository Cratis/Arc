// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_an_ignored_nested_immediate_append_refusal : given.an_in_process_executor
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => Execute(new IgnoreNestedFailure()));

    [Fact] void should_rethrow_the_refusal() => _exception.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_return_a_failed_nested_result() => _counter.NestedResult!.IsSuccess.ShouldBeFalse();
    [Fact] void should_allow_the_outer_handler_to_ignore_the_result() => _counter.OuterHandlerCompleted.ShouldBeTrue();
    [Fact] void should_complete_the_rolled_back_unit() => _counter.Unit!.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_enroll_a_receipt() => _counter.Unit!.EffectReceipt.ShouldBeNull();
    [Fact] async Task should_not_record_a_receipt() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
    [Fact] async Task should_not_append_any_event() => (await _store.EventLog.GetFromSequenceNumber(0)).ShouldBeEmpty();
}
