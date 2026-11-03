// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class and_read_model_key_resolution_is_denied : given.a_throwing_command_with_exception_mapping
{
    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Throws(new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.Missing));
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_call_the_mapper() => _mapper.DidNotReceiveWithAnyArgs().TryMap(Arg.Any<CommandContext>(), Arg.Any<Exception>(), out _);
}
