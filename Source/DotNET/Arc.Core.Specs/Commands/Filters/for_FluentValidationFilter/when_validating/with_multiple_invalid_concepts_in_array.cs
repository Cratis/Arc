// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_multiple_invalid_concepts_in_array : given.a_fluent_validation_filter
{
    CommandResult _result;

    void Establish()
    {
        var command = new SendCommand([
            new CandidateSend(new HourlyRate(decimal.Zero)),
            new CandidateSend(new HourlyRate(decimal.Zero))
        ]);
        _context = new CommandContext(_correlationId, typeof(SendCommand), command, [], new());

        var hourlyRateValidator = new HourlyRateValidator();
        _discoverableValidators.TryGet(typeof(HourlyRate), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = hourlyRateValidator;
                return true;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_surface_an_error_for_each_invalid_concept() =>
        _result.ValidationResults.Count().ShouldEqual(2);

    record HourlyRate(decimal Value) : ConceptAs<decimal>(Value);
    record CandidateSend(HourlyRate CustomerHourlyRate);
    record SendCommand(CandidateSend[] Candidates);

    class HourlyRateValidator : ConceptValidator<HourlyRate>
    {
        public HourlyRateValidator() =>
            RuleFor(x => x.Value).GreaterThan(0m).WithMessage("Hourly rate must be positive");
    }
}
