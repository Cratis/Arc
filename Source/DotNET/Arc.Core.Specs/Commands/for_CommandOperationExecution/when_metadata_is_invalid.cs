// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_metadata_is_invalid : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), Substitute.For<ICommandOperation>()]));

    [Fact] void should_reject_before_the_first_external_call() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_report_a_diagnostic() => _result.ExceptionMessages.Single().ShouldContain("Execute");
    [Fact] void should_not_report_simulated_execution() => _result.OperationOutcomes.ShouldBeEmpty();
}
