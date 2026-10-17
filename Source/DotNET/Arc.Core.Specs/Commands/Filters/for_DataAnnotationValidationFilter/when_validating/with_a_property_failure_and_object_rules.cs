// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_a_property_failure_and_object_rules : given.a_data_annotation_validation_filter
{
    WithObjectRules _command;
    CommandResult _result;

    void Establish()
    {
        _command = new(new(string.Empty));
        _context = new(_correlationId, typeof(WithObjectRules), _command, [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_report_only_the_required_failure() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
    [Fact] void should_not_run_object_rules_after_a_property_failure() => _command.ValidationCalls.ShouldEqual(0);
}
