// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandsResultHandler.when_handling;

public class with_successful_commands : given.a_commands_result_handler
{
    TestCommand _command1;
    TestCommand _command2;
    CorrelationId _correlationId1;
    CorrelationId _correlationId2;

    void Establish()
    {
        _command1 = new TestCommand("cmd1");
        _command2 = new TestCommand("cmd2");
        _correlationId1 = CorrelationId.New();
        _correlationId2 = CorrelationId.New();

        _commandPipeline.Execute(_command1, _serviceProvider).Returns(CommandResult.Success(_correlationId1));
        _commandPipeline.Execute(_command2, _serviceProvider).Returns(CommandResult.Success(_correlationId2));
    }

    async Task Because() => await _handler.Handle(_reactorContext, _eventStore, new[] { _command1, _command2 });

    [Fact] void should_create_a_service_scope() => _serviceScopeFactory.Received(1).CreateScope();
    [Fact] void should_execute_first_command() => _commandPipeline.Received(1).Execute(_command1, _serviceProvider);
    [Fact] void should_execute_second_command() => _commandPipeline.Received(1).Execute(_command2, _serviceProvider);
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
}
