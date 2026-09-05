// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_command_context_carries_the_created_scope : given.a_command_pipeline_and_a_handler_for_command
{
    CommandContext _capturedContext = null!;

    void Establish() =>
        _commandFilters.OnExecution(Arg.Do<CommandContext>(context => _capturedContext = context))
            .Returns(CommandResult.Success(_correlationId));

    async Task Because() => await _commandPipeline.Execute(_command);

    [Fact] void should_carry_the_service_provider_from_the_created_scope() => _capturedContext.ServiceProvider.ShouldEqual(_serviceProvider);
}
