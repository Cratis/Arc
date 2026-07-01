// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.when_handling;

public class with_successful_command : given.a_command_result_handler
{
    TestCommand _command;
    CommandResult _commandResult;

    void Establish()
    {
        _command = new TestCommand("Test");
        _commandResult = CommandResult.Success(CorrelationId.New());
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_commandResult);
    }

    async Task Because() => await _handler.Handle(_reactorContext, _eventStore, _command);

    [Fact] void should_execute_command_through_pipeline() => _commandPipeline.Received(1).Execute(_command, _serviceProvider);
    [Fact] void should_create_service_scope() => _serviceScopeFactory.Received(1).CreateScope();
}
