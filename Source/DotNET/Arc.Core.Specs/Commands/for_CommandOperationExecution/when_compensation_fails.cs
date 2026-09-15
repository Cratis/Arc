// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_compensation_fails : given.an_operation_pipeline
{
    void Establish()
    {
        _log.FailOn = "B";
        _log.FailCompensationOn = "B";
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new given.NamedOperation("B")]));

    [Fact] void should_continue_with_earlier_compensators() => _log.Calls.ShouldEqual(["execute:A", "execute:B", "compensate:B", "compensate:A"]);
    [Fact] void should_keep_recovery_errors_distinct() => _result.ExceptionMessages.ShouldEqual(["original:B"]);
    [Fact] void should_record_the_compensator_failure() => _result.OperationOutcomes[1].CompensationFailure.ShouldEqual("recovery:B");
    [Fact] void should_report_incomplete_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Incomplete);
    [Fact] void should_count_unresolved_invocations() => _result.Recovery.UncompensatedCount.ShouldEqual(1);
}
