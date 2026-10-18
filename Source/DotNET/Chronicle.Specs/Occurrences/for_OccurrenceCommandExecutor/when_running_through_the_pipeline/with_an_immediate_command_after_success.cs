// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_an_immediate_command_after_success : given.an_in_process_executor
{
    CommandResult _later;

    async Task Because()
    {
        _result = await Execute(new RecordEffect("occurrence"));
        _later = await _services.GetRequiredService<ICommandPipeline>().Execute(new RecordEffect("later", Immediate: true), _services);
    }

    [Fact] void should_record_the_occurrence() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_allow_the_later_command() => _later.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_append_both_events() => (await _store.EventLog.GetFromSequenceNumber(0)).Count.ShouldEqual(2);
}
