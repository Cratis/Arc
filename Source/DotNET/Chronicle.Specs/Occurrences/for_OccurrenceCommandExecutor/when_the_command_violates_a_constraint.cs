// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Occurrences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_violates_a_constraint : given.an_occurrence_command_executor
{
    void Establish()
    {
        _commandResult = new CommandResult { CorrelationId = CorrelationId.New(), ValidationResults = [ValidationResult.Error("sensitive text", ["Name"], reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueName")] };
        CreateExecutor();
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_be_settled() => _result.IsSettled.ShouldBeTrue();
    [Fact] async Task should_record_a_refused_receipt() => (await StoredReceipt)!.Outcome.ShouldBeOfExactType<Refused>();
    [Fact] async Task should_not_echo_the_validation_message() => (await StoredReceipt)!.Response!.ToJsonString().ShouldNotContain("sensitive text");
    [Fact] void should_keep_the_command_result() => _result.CommandResult.ShouldEqual(_commandResult);
}
