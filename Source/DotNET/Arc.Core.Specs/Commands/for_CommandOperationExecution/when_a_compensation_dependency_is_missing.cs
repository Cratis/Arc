// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_compensation_dependency_is_missing : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new MissingRecoveryDependency()]));

    [Fact] void should_preflight_recovery_before_any_execution() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();

    public record MissingRecoveryDependency : ICommandOperation
    {
        public void Execute() { }
        public void Compensate(IFormatProvider missing) { }
    }
}
