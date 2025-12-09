// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_empty_object : given.a_fluent_validation_filter
{
    CommandResult _result;
    EmptyCommand _command;

    void Establish()
    {
        _command = new EmptyCommand();
        _context = new CommandContext(_correlationId, typeof(EmptyCommand), _command, [], new());

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
    [Fact] void should_attempt_to_get_validator_for_command() => _discoverableValidators.Received(1).TryGet(typeof(EmptyCommand), out Arg.Any<IValidator>());

    record EmptyCommand;
}