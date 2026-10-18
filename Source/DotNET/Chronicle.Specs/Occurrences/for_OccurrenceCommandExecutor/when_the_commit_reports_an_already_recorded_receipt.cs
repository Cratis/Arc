// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_commit_reports_an_already_recorded_receipt : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(1);
        _duringExecution = async () =>
        {
            // Another writer wins between the lookup and our commit.
            await _eventStore.Occurrences.RecordEffectReceipt(_key, new Refused(["winner"]), new JsonObject { ["winner"] = true });
            OccurrenceExecution.Current!.Enroll(_unitOfWork);
            await _unitOfWork.Commit();
            OccurrenceExecution.Current!.Committed(_unitOfWork);
            _commandResult.MergeWith(CommandResult.Error(_commandResult.CorrelationId, "A later scope failed after the winning receipt was observed"));
        };
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_already_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.AlreadyRecorded);
    [Fact] void should_return_the_stored_outcome() => _result.Outcome.ShouldBeOfExactType<Refused>();
    [Fact] void should_return_the_stored_response() => ((bool)_result.Response!["winner"]!).ShouldBeTrue();
    [Fact] void should_discard_our_command_result() => _result.CommandResult.ShouldBeNull();
}
