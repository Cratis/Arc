// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_an_immediate_command_after_refusal : given.an_in_process_executor
{
    Exception _exception;
    CommandResult _later;

    async Task Because()
    {
        _exception = await Catch.Exception(() => Execute(new RecordEffect("occurrence", Immediate: true)));
        _later = await _services.GetRequiredService<ICommandPipeline>().Execute(new RecordEffect("later", Immediate: true), _services);
    }

    [Fact] void should_rethrow_the_refusal() => _exception.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_allow_the_later_command() => _later.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_append_only_the_later_event() => (await _store.EventLog.GetFromSequenceNumber(0)).Single().Content.ShouldEqual(new EffectHappened("later"));
    [Fact] void should_run_both_handlers() => _counter.Calls.ShouldEqual(2);
    [Fact] async Task should_leave_the_occurrence_unreceipted() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
}
