// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_a_non_concept_property_failure_beside_a_concept : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithMixedProperties), new WithMixedProperties(new("Valid"), string.Empty), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_preserve_required_priority_and_the_authored_message() => _result.ValidationResults.Single().Message.ShouldEqual("Label is required");
    [Fact] void should_report_the_client_field_once() => _result.ValidationResults.Single().Members.ShouldContainOnly("label");
}
