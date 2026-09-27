// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_the_command_declares_a_blocking_severity : given.a_command_with_validation_policy
{
    CommandResult _result;
    CommandResult _unknown;

    async Task Because()
    {
        _failureSeverity = ValidationResultSeverity.Information;
        _result = await _commandPipeline.Validate(_command, _serviceProvider, ValidationResultSeverity.Error);
        _failureSeverity = ValidationResultSeverity.Unknown;
        _unknown = await _commandPipeline.Validate(_command);
    }

    [Fact] void should_reject_information_even_with_a_permissive_threshold() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_information_severity() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Information);
    [Fact] void should_reject_unknown_severity_in_an_owned_scope() => _unknown.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_unknown_severity_and_message() => (_unknown.ValidationResults.Single().Severity == ValidationResultSeverity.Unknown && _unknown.ValidationResults.Single().Message == "Keep this message").ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
