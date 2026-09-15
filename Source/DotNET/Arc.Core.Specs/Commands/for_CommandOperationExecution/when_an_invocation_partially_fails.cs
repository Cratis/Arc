// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_an_invocation_partially_fails : given.an_operation_pipeline
{
    void Establish() => _log.FailOn = "C";
    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new given.NamedOperation("B"), new given.NamedOperation("C"), new given.NamedOperation("D")]));

    [Fact] void should_reverse_only_started_invocations_including_the_failure() => _log.Calls.ShouldEqual(["execute:A", "execute:B", "execute:C", "compensate:C", "compensate:B", "compensate:A"]);
    [Fact] void should_preserve_the_original_failure() => _result.ExceptionMessages.ShouldEqual(["original:C"]);
    [Fact] void should_not_turn_recovery_into_command_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_record_partial_execution() => _result.OperationOutcomes[2].ExecutionCompleted.ShouldBeFalse();
    [Fact] void should_identify_the_failing_invocation() => _log.Failures[0].IsFailingInvocation.ShouldBeTrue();
    [Fact] void should_identify_other_invocations() => _log.Failures[1].IsFailingInvocation.ShouldBeFalse();
    [Fact] void should_record_observed_recovery_completion() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);
}
