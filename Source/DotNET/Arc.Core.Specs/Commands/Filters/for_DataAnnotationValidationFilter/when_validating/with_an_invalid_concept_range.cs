// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_an_invalid_concept_range : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithAmount), new WithAmount(new(101)), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_field_once() => _result.ValidationResults.Single().Members.ShouldContainOnly("amount");
}
