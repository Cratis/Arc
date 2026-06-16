// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_cancellation_token_is_supplied : given.a_command_pipeline_and_a_handler_for_command
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    CommandContext _capturedContext = null!;

    void Establish() =>
        _commandFilters.OnExecution(Arg.Do<CommandContext>(context => _capturedContext = context))
            .Returns(CommandResult.Success(_correlationId));

    async Task Because() => await _commandPipeline.Execute(
        _command,
        _serviceProvider,
        cancellationToken: _cancellationTokenSource.Token);

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_carry_the_cancellation_token_on_the_context() => _capturedContext.CancellationToken.ShouldEqual(_cancellationTokenSource.Token);
}
