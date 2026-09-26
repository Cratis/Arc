// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_caller_is_stricter_than_the_command : given.a_command_with_validation_policy
{
    [Command]
    [BlockOnValidationSeverity(ValidationResultSeverity.Error)]
    record ErrorPolicyCommand;

    CommandResult _result;

    async Task Because()
    {
        _command = new ErrorPolicyCommand();
        _commandHandlerProviders.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _failureSeverity = ValidationResultSeverity.Warning;
        _result = await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Information);
    }

    [Fact] void should_reject_the_warning() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_original_severity() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Warning);
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
