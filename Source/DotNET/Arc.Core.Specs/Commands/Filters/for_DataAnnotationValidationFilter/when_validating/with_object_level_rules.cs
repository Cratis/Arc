// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_object_level_rules : given.a_data_annotation_validation_filter
{
    WithObjectRules _command;
    CommandResult _result;

    void Establish()
    {
        _command = new("Valid");
        _context = new(_correlationId, typeof(WithObjectRules), _command, [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_run_the_object_rules_once() => _command.ValidationCalls.ShouldEqual(1);
    [Fact] void should_preserve_the_object_rule_message() => _result.ValidationResults.Single().Message.ShouldEqual("Object rule rejected");
    [Fact] void should_normalize_each_member_segment() => _result.ValidationResults.Single().Members.ShouldContainOnly("items[0].name");
}
