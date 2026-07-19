// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_a_deeply_nested_validator_reporting_an_inner_member : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _innerValidator;
    OuterCommand _command;

    void Establish()
    {
        _command = new OuterCommand(new Middle(new Inner("value")));
        _context = new CommandContext(_correlationId, typeof(OuterCommand), _command, [], new());

        _innerValidator = Substitute.For<IValidator>();
        _innerValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Value", "Inner value is invalid")]));

        _discoverableValidators.TryGet(typeof(Inner), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _innerValidator;
                return true;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_compose_the_full_camel_cased_path() => _result.ValidationResults.First().Members.ShouldContain("middle.inner.Value");

    record OuterCommand(Middle Middle);
    record Middle(Inner Inner);
    record Inner(string Value);
}
