// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_a_derived_comparison_and_equal_concepts : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithDerivedComparison), new WithDerivedComparison(new("Same"), new("Same")), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_accept_equal_concepts() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_validation_errors() => _result.ValidationResults.ShouldBeEmpty();
}
