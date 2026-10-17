// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_succeeds_without_events : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(0);
        _duringExecution = () =>
        {
            OccurrenceExecution.Current!.Enroll(_unitOfWork);
            return Task.CompletedTask;
        };
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] async Task should_record_a_no_events_receipt() => (await StoredReceipt)!.Outcome.ShouldBeOfExactType<NoEvents>();
    [Fact] void should_report_a_no_events_outcome() => _result.Outcome.ShouldBeOfExactType<NoEvents>();
    [Fact] void should_not_enroll_a_receipt_in_the_unit_of_work() => _unitOfWork.EffectReceipt.ShouldBeNull();
}
