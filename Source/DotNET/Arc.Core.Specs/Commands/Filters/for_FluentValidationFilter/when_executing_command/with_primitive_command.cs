// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_primitive_command : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _validator;
    FluentValidationResult _validationResult;

    void Establish()
    {
        const int command = 42;
        _context = new CommandContext(_correlationId, typeof(int), command, [], new());

        _validator = Substitute.For<IValidator>();
        _validationResult = new FluentValidationResult();

        _discoverableValidators.TryGet(typeof(int), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _validator;
                return true;
            });

        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(_validationResult);
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_call_validator_once() => _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
}