// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_a_failure_follows_a_receipted_commit : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(1);
        _duringExecution = async () =>
        {
            OccurrenceExecution.Current!.Enroll(_unitOfWork);
            await _unitOfWork.Commit();
            OccurrenceExecution.Current!.Committed(_unitOfWork);
            _commandResult.MergeWith(CommandResult.Error(_commandResult.CorrelationId, "A later execution scope failed"));
        };
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_settle_from_the_durable_receipt() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_report_the_recorded_append() => _result.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] void should_keep_the_late_failure_available_to_the_caller() => _result.CommandResult!.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_keep_the_recorded_receipt() => (await StoredReceipt)!.Outcome.ShouldBeOfExactType<Appended>();
}
