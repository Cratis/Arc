// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_a_null_tag_property_is_rejected_by_validation : given.a_pipeline_with_deferred_tags
{
    CommandResult _result;
    ValidationResult _validation;
    void Establish()
    {
        _validation = ValidationResult.Error("Project is required.", [nameof(TaggedCommand.ProjectId)]);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(new CommandResult { CorrelationId = _correlationId, ValidationResults = [_validation] });
    }
    async Task Because() => _result = await _commandPipeline.Execute(_taggedCommand, _serviceProvider);

    [Fact] void should_return_the_validation_result() => _result.ValidationResults.ShouldEqual([_validation]);
    [Fact] void should_not_throw_a_missing_tag_value_error() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_write_tags_while_building_context() => _builtValues.ContainsKey(WellKnownCommandContextKeys.EventTags).ShouldBeFalse();
    [Fact] void should_not_evaluate_command_tag_providers() => _taggedCommand.TagReads.ShouldEqual(0);
    [Fact] void should_not_evaluate_application_tag_providers() => _tagProvider.DidNotReceive().GetEventTags(Arg.Any<object>());
}
