// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_there_is_no_handler_for_a_typed_result : given.a_command_pipeline
{
    CommandResult<string> _result;

    async Task Because() => _result = await _commandPipeline.Execute<string>("something", _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_missing_handler() => _result.IsMissingHandler.ShouldBeTrue();
}
