// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_stream_completion : given.an_in_process_executor
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => Execute(new CompleteAStream()));

    [Fact] void should_rethrow_the_programming_error_caught_by_the_pipeline() => _exception.ShouldBeOfExactType<OccurrenceCommandCannotCompleteStreams>();
    [Fact] async Task should_not_record_a_receipt() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
    [Fact] async Task should_not_append_events() => (await _store.EventLog.GetFromSequenceNumber(0)).ShouldBeEmpty();
}
