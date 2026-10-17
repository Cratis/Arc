// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_a_null_required_concept : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithRequiredName), new WithRequiredName(null), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_null_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_field_once() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
}
