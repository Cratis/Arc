// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class with_a_declining_mapper : given.a_throwing_command_with_exception_mapping
{
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_keep_the_original_failure() => _result.ExceptionMessages.ShouldContainOnly(_exception.Message);
    [Fact] void should_leave_validation_unchanged() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_consult_the_mapper() => _mapper.Received(1).TryMap(Arg.Any<CommandContext>(), _exception, out Arg.Any<CommandResult>());
}
