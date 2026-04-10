// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_required_property_that_is_null : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() =>
        _context = new CommandContext(_correlationId, typeof(CommandWithRequiredProperty), new CommandWithRequiredProperty(null!), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_return_successful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_have_validation_result_for_the_required_property() => _result.ValidationResults.First().Members.ShouldContain(nameof(CommandWithRequiredProperty.Name));

    record CommandWithRequiredProperty([property: Required] string Name);
}
