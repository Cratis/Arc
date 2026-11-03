// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_snapshot_membership_is_denied : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;

    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
    {
        // Strict membership is off and the severity is lenient: the denial must still not become a validation failure.
        await _invoker.Invoke("value", new ReadingValidator(), string.Empty);
        return CommandResult.Success(_correlationId);
    });

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_a_validation_failure() => _result.ValidationResults.ShouldBeEmpty();
}
