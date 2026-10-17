// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_has_no_events_but_protected_reads : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(0);
        _unitOfWork.EnforceReadProtection();
        var owner = ((UnitOfWork)_unitOfWork).ClaimDecisionReadCommitOwnership();
        _duringExecution = async () =>
        {
            OccurrenceExecution.Current!.Enroll(_unitOfWork);
            await ((UnitOfWork)_unitOfWork).CommitAsOwner(owner);
            OccurrenceExecution.Current!.Committed(_unitOfWork);
        };
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_report_a_no_events_outcome() => _result.Outcome.ShouldBeOfExactType<NoEvents>();
}
