// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tags_alone : given.a_command_pipeline_with_tag_handlers
{
    CommandResult _result;
    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(new EventTags(_tags));
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_return_tags_to_the_client() => _result.ShouldBeOfExactType<CommandResult>();
    [Fact] void should_not_append_an_event() => _eventLog.ReceivedCalls().ShouldBeEmpty();
}
