// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_a_tagged_command_is_unauthorized : given.a_pipeline_with_deferred_tags
{
    CommandResult _result;
    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(_correlationId));
    async Task Because() => _result = await _commandPipeline.Execute(_taggedCommand, _serviceProvider);

    [Fact] void should_return_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_throw_a_missing_tag_value_error() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_evaluate_command_tag_providers() => _taggedCommand.TagReads.ShouldEqual(0);
    [Fact] void should_not_evaluate_application_tag_providers() => _tagProvider.DidNotReceive().GetEventTags(Arg.Any<object>());
}
