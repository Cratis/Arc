// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class with_a_throwing_mapper : given.a_throwing_command_with_exception_mapping
{
    void Establish() => _mapper.TryMap(Arg.Any<CommandContext>(), _exception, out Arg.Any<CommandResult>()).Throws(new Exception("mapping failed"));
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_fail_instead_of_losing_scope_completion() => _result.ExceptionMessages.ShouldContainOnly("mapping failed");
    [Fact] async Task should_complete_execution_scopes_with_failure() => await _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Is<CommandResult>(result => !result.IsSuccess));
}
