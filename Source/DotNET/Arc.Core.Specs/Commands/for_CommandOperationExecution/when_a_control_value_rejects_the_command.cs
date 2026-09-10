// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_control_value_rejects_the_command : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Rejected());

    [Fact] void should_start_no_operations() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_preserve_rejection_even_after_selecting_a_response() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_clear_the_client_response() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_report_no_recovery_needed() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.NotNeeded);

    public record Rejected
    {
        public (given.NamedOperation Operation, string Response, CommandResult Rejection) Handle() =>
            (new("A"), "response", CommandResult.Error(Cratis.Execution.CorrelationId.NotSet, "rejected"));
    }
}
