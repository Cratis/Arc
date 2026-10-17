// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_type_level_rules : given.a_data_annotation_validation_filter
{
    WithTypeRules _command;
    CommandResult _result;

    void Establish()
    {
        _command = new(new("Valid"));
        _context = new(_correlationId, typeof(WithTypeRules), _command, [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_preserve_the_type_rule_message() => _result.ValidationResults.Single().Message.ShouldEqual("Object annotation rejected");
    [Fact] void should_report_the_field_once() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
    [Fact] void should_not_run_object_rules_after_a_type_failure() => _command.ValidationCalls.ShouldEqual(0);
}
