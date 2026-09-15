// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_no_compensator_is_declared : given.an_operation_pipeline
{
    void Establish() => _log.FailOn = "B";
    async Task Because() => _result = await Run(new given.OperationCommand([new Irreversible(), new given.NamedOperation("B")]));

    [Fact] void should_report_unavailable_recovery() => _result.OperationOutcomes[0].Compensation.ShouldEqual(CommandOperationCompensation.NotAvailable);
    [Fact] void should_still_compensate_other_invocations() => _log.Calls.ShouldEqual(["execute:A", "execute:B", "compensate:B"]);
    [Fact] void should_not_claim_complete_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Incomplete);

    public record Irreversible : ICommandOperation
    {
        public Task Execute(given.OperationLog log, CancellationToken cancellationToken) => log.Execute("A", cancellationToken);
    }
}
