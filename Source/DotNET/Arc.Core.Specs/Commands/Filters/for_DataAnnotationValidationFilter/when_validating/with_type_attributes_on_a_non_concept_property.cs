// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_type_attributes_on_a_non_concept_property : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() => _context = new(_correlationId, typeof(WithAnnotatedProperty), new WithAnnotatedProperty(new("Valid"), new("Valid")), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_run_the_property_types_attributes() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_accept_the_command() => _result.IsSuccess.ShouldBeTrue();
}
