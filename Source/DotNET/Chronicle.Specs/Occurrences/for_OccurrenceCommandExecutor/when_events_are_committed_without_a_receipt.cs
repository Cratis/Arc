// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_events_are_committed_without_a_receipt : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(1);
        _duringExecution = () =>
        {
            OccurrenceExecution.Current!.Enroll(_unitOfWork);

            // The commit never reports a receipt back.
            OccurrenceExecution.Current!.Committed(_unitOfWork);
            return Task.CompletedTask;
        };
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<OccurrenceCommandReceiptNotCommitted>();
    [Fact] async Task should_not_record_a_receipt() => (await StoredReceipt).ShouldBeNull();
}
