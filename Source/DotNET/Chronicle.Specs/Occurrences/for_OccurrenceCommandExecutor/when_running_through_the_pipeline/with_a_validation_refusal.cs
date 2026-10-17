// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_a_validation_refusal : given.an_in_process_executor
{
    async Task Because() => _result = await Execute(new RecordEffect(string.Empty));

    [Fact] void should_record_a_refused_outcome() => _result.Outcome.ShouldBeOfExactType<Refused>();
    [Fact] async Task should_persist_the_refused_receipt() => (await _store.Occurrences.GetEffectReceipt(_key))!.Outcome.ShouldBeOfExactType<Refused>();
    [Fact] void should_not_run_the_handler() => _counter.Calls.ShouldEqual(0);
    [Fact] async Task should_not_append_events() => (await _store.EventLog.GetFromSequenceNumber(0)).ShouldBeEmpty();
    [Fact] void should_identify_the_refused_member() => _result.Response!.ToJsonString().ShouldContain("refused: value");
}
