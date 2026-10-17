// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_an_empty_required_compared_concept : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithRequiredComparison), new WithRequiredComparison(new("Valid"), new(string.Empty)), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_only_report_the_required_failure() => _result.ValidationResults.Single().Message.ShouldEqual("Confirmation is required");
    [Fact] void should_report_the_confirmation_field() => _result.ValidationResults.Single().Members.ShouldContainOnly("confirmation");
}
