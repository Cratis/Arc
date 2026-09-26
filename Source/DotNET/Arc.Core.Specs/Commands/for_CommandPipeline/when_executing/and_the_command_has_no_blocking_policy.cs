// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_has_no_blocking_policy : given.a_command_with_validation_policy
{
    CommandResult _defaultResult;
    CommandResult _callerResult;
    CommandResult _unknownResult;

    void Establish()
    {
        _command = new LegacyCommand();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(_command, out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
    }

    async Task Because()
    {
        _failureSeverity = ValidationResultSeverity.Warning;
        _defaultResult = await _commandPipeline.Execute(_command, _serviceProvider);
        _callerResult = await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Information);
        _failureSeverity = ValidationResultSeverity.Unknown;
        _unknownResult = await _commandPipeline.Validate(_command, _serviceProvider);
    }

    [Fact] void should_continue_with_warning_by_default() => _defaultResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_warning_when_the_caller_requires_it() => _callerResult.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Warning);
    [Fact] void should_still_allow_unknown_without_an_attribute() => _unknownResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_call_the_handler_for_the_allowed_warning() => _handler.Received(1).Handle(Arg.Any<CommandContext>());
}
