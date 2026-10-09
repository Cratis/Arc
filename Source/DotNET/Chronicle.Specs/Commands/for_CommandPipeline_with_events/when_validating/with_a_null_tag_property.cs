// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_validating;

public class with_a_null_tag_property : given.a_pipeline_with_deferred_tags
{
    CommandResult _result;
    async Task Because() => _result = await _commandPipeline.Validate(_taggedCommand, _serviceProvider);

    [Fact] void should_return_the_filter_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_write_tags_while_building_context() => _builtValues.ContainsKey(WellKnownCommandContextKeys.EventTags).ShouldBeFalse();
    [Fact] void should_not_evaluate_command_tag_providers() => _taggedCommand.TagReads.ShouldEqual(0);
    [Fact] void should_not_evaluate_application_tag_providers() => _tagProvider.DidNotReceive().GetEventTags(Arg.Any<object>());
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
