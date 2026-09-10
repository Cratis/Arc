// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_compensation_ignores_a_nested_command_failure : Specification
{
    CommandScenario<given.AttemptNestedRecoveryBatch> _scenario;
    given.INestedOperationProbe _probe;
    CommandResult _result;

    void Establish()
    {
        _probe = Substitute.For<given.INestedOperationProbe>();
        _scenario = new();
        _scenario.Services.AddSingleton(_probe);
    }

    async Task Because() => _result = await _scenario.Execute(new given.AttemptNestedRecoveryBatch());

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_not_execute_the_child() => _probe.DidNotReceive().Child();

    [Fact] void should_not_claim_the_rejected_compensation_completed() =>
        _scenario.Operations[1].Compensation.ShouldEqual(CommandOperationCompensation.Failed);

    [Fact] void should_continue_the_remaining_compensations() => _probe.Received(1).Compensated();

    [Fact] void should_record_the_successful_remaining_compensation() =>
        _scenario.Operations[0].Compensation.ShouldEqual(CommandOperationCompensation.Completed);

    [Fact] void should_report_incomplete_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Incomplete);

    [Fact] void should_preserve_the_original_failure() =>
        _result.ExceptionMessages.ShouldContain("The forward operation failed before nested compensation.");
}
