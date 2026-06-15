// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

/// <summary>
/// The dry-run validation endpoint (the <c>/validate</c> path) runs the same filters as execution, so it must also
/// carry the command-scoped service provider — otherwise a read-model-injecting validator would fail during a
/// validation-only request while succeeding during execution.
/// </summary>
public class and_command_context_carries_the_service_provider : given.a_command_pipeline_and_a_handler_for_command
{
    CommandContext _capturedContext = null!;

    void Establish() =>
        _commandFilters.OnExecution(Arg.Do<CommandContext>(context => _capturedContext = context))
            .Returns(CommandResult.Success(_correlationId));

    async Task Because() => await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_carry_the_service_provider_on_the_context() => _capturedContext.ServiceProvider.ShouldEqual(_serviceProvider);
}
