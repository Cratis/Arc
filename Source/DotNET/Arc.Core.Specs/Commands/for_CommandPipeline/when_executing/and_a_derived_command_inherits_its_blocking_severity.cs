// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_derived_command_inherits_its_blocking_severity : given.a_command_with_validation_policy
{
    CommandResult _result;

    async Task Because()
    {
        _command = new DerivedStrictCommand();
        _commandHandlerProviders.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _failureSeverity = ValidationResultSeverity.Warning;
        _result = await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Error);
    }

    [Fact] void should_reject_a_warning_despite_a_permissive_caller() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_warning() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Warning);
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
