// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_is_not_authorized : given.an_occurrence_command_executor
{
    void Establish()
    {
        _commandResult = CommandResult.Unauthorized(CorrelationId.New(), "no");
        CreateExecutor();
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_failed() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Failed);
    [Fact] void should_not_be_settled() => _result.IsSettled.ShouldBeFalse();
    [Fact] async Task should_not_record_a_receipt() => (await StoredReceipt).ShouldBeNull();
    [Fact] void should_have_no_outcome() => _result.Outcome.ShouldBeNull();
    [Fact] void should_return_the_command_result() => _result.CommandResult.ShouldEqual(_commandResult);
}
