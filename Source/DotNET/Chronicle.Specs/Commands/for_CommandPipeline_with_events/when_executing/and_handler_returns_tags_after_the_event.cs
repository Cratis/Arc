// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_handler_returns_tags_after_the_event : given.a_command_pipeline_with_tag_handlers
{
    CommandResult _result;
    TestEvent _event;

    void Establish()
    {
        _event = new("tagged");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns((_event, new EventTags(_tags)));
    }
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_apply_tags_before_appending_the_event() => _eventLog.Received(1).AppendWithNamedTags(_command.EventSourceId, _event, Arg.Is<IEnumerable<NamedTag>>(_ => _.SequenceEqual(_tags)));
    [Fact] void should_not_treat_tags_as_the_response() => _result.ShouldBeOfExactType<CommandResult>();
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
}
