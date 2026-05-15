// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_valid_concepts_in_array : given.a_fluent_validation_filter
{
    CommandResult _result;

    void Establish()
    {
        var command = new SendCommand([
            new CandidateSend(new HourlyRate(50m)),
            new CandidateSend(new HourlyRate(100m))
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

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_validation_results() => _result.ValidationResults.ShouldBeEmpty();

    record HourlyRate(decimal Value) : ConceptAs<decimal>(Value);
    record CandidateSend(HourlyRate CustomerHourlyRate);
    record SendCommand(CandidateSend[] Candidates);

    class HourlyRateValidator : ConceptValidator<HourlyRate>
    {
        public HourlyRateValidator() =>
            RuleFor(x => x.Value).GreaterThan(0m).WithMessage("Hourly rate must be positive");
    }
}
