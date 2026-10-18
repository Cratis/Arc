// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_an_immediate_append : given.an_in_process_executor
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => Execute(new RecordEffect("must-not-commit", Immediate: true)));

    [Fact] void should_surface_the_failed_keyed_execution() => _exception.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] async Task should_not_record_a_receipt() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
    [Fact] async Task should_refuse_before_appending_any_event() => (await _store.EventLog.GetFromSequenceNumber(0)).ShouldBeEmpty();
}
