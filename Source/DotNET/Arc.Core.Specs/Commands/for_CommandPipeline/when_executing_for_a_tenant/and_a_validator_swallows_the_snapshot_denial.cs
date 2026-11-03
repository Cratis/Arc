// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_validator_swallows_the_snapshot_denial : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;

    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
    {
        try
        {
            await _invoker.Invoke("value", new ReadingValidator(), string.Empty);
        }
        catch
        {
            // A validator or filter that swallows the failure must not hide it.
        }

        return CommandResult.Success(_correlationId);
    });

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_still_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
}
