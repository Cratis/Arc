// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_concept_as_direct_property : given.a_fluent_validation_filter
{
    CommandResult _result;

    void Establish()
    {
        var command = new CommandWithConcept(new Rate(decimal.Zero));
        _context = new CommandContext(_correlationId, typeof(CommandWithConcept), command, [], new());

        var rateValidator = new RateValidator();
        _discoverableValidators.TryGet(typeof(Rate), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = rateValidator;
                return true;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_surface_the_concept_validation_error() =>
        _result.ValidationResults.ShouldContain(vr => vr.Message == "Rate must be positive");

    record Rate(decimal Value) : ConceptAs<decimal>(Value);
    record CommandWithConcept(Rate Rate);

    class RateValidator : ConceptValidator<Rate>
    {
        public RateValidator() =>
            RuleFor(x => x.Value).GreaterThan(0m).WithMessage("Rate must be positive");
    }
}
