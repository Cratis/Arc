// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating;

public class with_nullable_property_without_required_attribute : given.a_data_annotation_validation_filter
{
    CommandResult _result;

    void Establish() =>
        _context = new CommandContext(_correlationId, typeof(CommandWithNullableProperty), new CommandWithNullableProperty("Alice", null), [], new());

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();

    record CommandWithNullableProperty(string Name, string? OptionalDescription);
}
