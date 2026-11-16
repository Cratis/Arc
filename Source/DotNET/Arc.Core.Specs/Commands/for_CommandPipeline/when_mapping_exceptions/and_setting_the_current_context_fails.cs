// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class and_setting_the_current_context_fails : given.a_throwing_command_with_exception_mapping
{
    void Establish()
    {
        _commandContextModifier.When(modifier => modifier.SetCurrent(Arg.Any<CommandContext>())).Throw(_exception);
        _mapper.TryMap(Arg.Any<CommandContext>(), _exception, out Arg.Any<CommandResult>()).Returns(call =>
        {
            call[2] = _mapped;
            return true;
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_the_mapped_rejection() => _result.ValidationResults.ShouldContainOnly(_mapped.ValidationResults.Single());
    [Fact] void should_not_expose_the_original_message() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_map_against_the_command_context() => _mapper.Received(1).TryMap(Arg.Is<CommandContext>(context => context.Type == _command.GetType() && context.CorrelationId == _correlationId), _exception, out Arg.Any<CommandResult>());
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
