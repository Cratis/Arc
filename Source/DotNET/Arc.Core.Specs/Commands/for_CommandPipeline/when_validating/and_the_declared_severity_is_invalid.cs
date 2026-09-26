// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_the_declared_severity_is_invalid : given.a_command_with_validation_policy
{
    CommandResult _result;
    Exception _exception;

    async Task Because()
    {
        _command = new InvalidPolicyCommand();
        _commandHandlerProviders.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _exception = Catch.Exception(() => CommandValidationResults.ForCommand(typeof(InvalidPolicyCommand), null));
        _result = await _commandPipeline.Validate(_command, _serviceProvider);
    }

    [Fact] void should_throw_invalid_command_validation_severity() => _exception.ShouldBeOfExactType<InvalidCommandValidationSeverity>();
    [Fact] void should_fail_with_the_declared_severity_exception() => _result.ExceptionMessages.Single().ShouldEqual(new InvalidCommandValidationSeverity(typeof(InvalidPolicyCommand)).Message);
    [Fact] void should_not_run_validation_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
