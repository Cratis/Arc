// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_different_compared_strings_beside_a_concept : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithComparedStrings), new WithComparedStrings(new("Valid"), "First", "Second"), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_different_strings() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_the_comparison_message() => _result.ValidationResults.Single().Message.ShouldEqual("Labels must match");
    [Fact] void should_report_the_confirmation_field() => _result.ValidationResults.Single().Members.ShouldContainOnly("confirmation");
}
