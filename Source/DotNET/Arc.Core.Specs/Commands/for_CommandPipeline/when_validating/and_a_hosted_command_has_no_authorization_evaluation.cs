// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_hosted_command_has_no_authorization_evaluation : given.a_hosted_command_with_missing_authorization_evaluation
{
    CommandResult _result;

    async Task Because() => _result = await _commandPipeline.ValidateHosted(new ProtectedCommand(), _serviceProvider, null, CancellationToken.None);

    [Fact] void should_report_unauthorized_rather_than_an_error() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_run_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
