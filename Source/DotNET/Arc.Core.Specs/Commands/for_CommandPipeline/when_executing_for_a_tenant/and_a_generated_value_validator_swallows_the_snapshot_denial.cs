// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_generated_value_validator_swallows_the_snapshot_denial : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;

    void Establish()
    {
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(async _ =>
            {
                try
                {
                    await _invoker.Invoke("value", new ReadingValidator(), string.Empty);
                }
                catch
                {
                    // A context-free generated value whose validator swallows the failure must not hide it.
                }

                var failed = CommandResult.Error(_correlationId, "rejected");
                return new CommandHandlerArgumentResolution([], failed);
            });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
