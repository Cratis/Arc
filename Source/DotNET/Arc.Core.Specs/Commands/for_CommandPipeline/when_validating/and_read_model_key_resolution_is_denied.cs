// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_read_model_key_resolution_is_denied : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Throws(new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.Missing));
    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_exceptions() => _result.HasExceptions.ShouldBeFalse();
}
