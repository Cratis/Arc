// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_a_receipt_already_exists : given.an_occurrence_command_executor
{
    EffectReceipt _stored;

    async Task Establish()
    {
        CreateExecutor();
        var recorded = await _eventStore.Occurrences.RecordEffectReceipt(_key, new Refused(["earlier"]), new JsonObject { ["stored"] = true });
        _stored = recorded.Receipt;
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_already_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.AlreadyRecorded);
    [Fact] void should_return_the_stored_outcome() => _result.Outcome.ShouldBeOfExactType<Refused>();
    [Fact] void should_return_the_stored_response() => ((bool)_result.Response!["stored"]!).ShouldBeTrue();
    [Fact] void should_be_settled() => _result.IsSettled.ShouldBeTrue();
    [Fact] void should_not_run_the_command() => _pipelineCalls.ShouldEqual(0);
    [Fact] void should_not_return_a_command_result() => _result.CommandResult.ShouldBeNull();
}
