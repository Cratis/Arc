// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class and_reactor_is_marked_to_execute_as_system : given.a_command_side_effect_executor
{
    TestCommand _command;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandPipeline.Execute(_command, _serviceProvider).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => await _executor.Execute([_command], typeof(SystemReactor));

    [Fact] void should_execute_as_system_with_the_declared_roles() => _systemExecution.Received(1).AsSystem("Administrator");
}
