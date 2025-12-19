// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_multiple_levels_of_nesting : given.a_fluent_validation_filter
{
    CommandResult _result;
    ComplexCommand _command;

    void Establish()
    {
        var deeplyNested = new DeeplyNestedObject("DeepValue");
        var nested = new NestedObject("NestedValue", deeplyNested);
        _command = new ComplexCommand("CommandName", nested);
        _context = new CommandContext(_correlationId, typeof(ComplexCommand), _command, [], new());

        // No validators found for any type
        _discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>()).Returns(false);
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_attempt_to_get_validator_for_command() => _discoverableValidators.Received(1).TryGet(typeof(ComplexCommand), out Arg.Any<IValidator>());
    [Fact] void should_attempt_to_get_validator_for_nested_object() => _discoverableValidators.Received(1).TryGet(typeof(NestedObject), out Arg.Any<IValidator>());
    [Fact] void should_attempt_to_get_validator_for_deeply_nested_object() => _discoverableValidators.Received(1).TryGet(typeof(DeeplyNestedObject), out Arg.Any<IValidator>());
    [Fact] void should_attempt_to_get_validator_for_string_properties() => _discoverableValidators.Received().TryGet(typeof(string), out Arg.Any<IValidator>());

    record ComplexCommand(string Name, NestedObject Nested);
    record NestedObject(string Value, DeeplyNestedObject Deep);
    record DeeplyNestedObject(string DeepValue);
}