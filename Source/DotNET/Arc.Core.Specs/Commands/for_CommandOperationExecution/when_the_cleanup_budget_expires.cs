// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_the_cleanup_budget_expires : given.an_operation_pipeline
{
    void Establish()
    {
        _log.FailOn = "B";
        _log.WaitForCleanupCancellationOn = "B";
        _services.Configure<CommandOperationOptions>(options => options.CompensationTimeout = TimeSpan.FromMilliseconds(100));
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new given.NamedOperation("B")]));

    [Fact] void should_await_the_cooperative_compensator() => _result.OperationOutcomes[1].Compensation.ShouldEqual(CommandOperationCompensation.Failed);
    [Fact] void should_not_start_another_callback_after_the_deadline() => _result.OperationOutcomes[0].Compensation.ShouldEqual(CommandOperationCompensation.BudgetExpired);
    [Fact] void should_report_incomplete_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Incomplete);
    [Fact] void should_preserve_the_original_failure() => _result.ExceptionMessages.ShouldEqual(["original:B"]);
}
