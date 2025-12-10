// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_there_is_no_handler : given.a_command_pipeline
{
    CommandResult _result;

    async Task Because() => _result = await _commandPipeline.Validate("something");

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_set_current_command_context() => _commandContextModifier.DidNotReceive().SetCurrent(Arg.Any<CommandContext>());
}
