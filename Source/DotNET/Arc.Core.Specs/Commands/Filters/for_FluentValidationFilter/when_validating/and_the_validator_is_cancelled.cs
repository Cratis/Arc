// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class and_the_validator_is_cancelled : given.a_fluent_validation_filter
{
    Exception _exception;

    void Establish()
    {
        var command = new SomeCommand("value");
        _context = new CommandContext(_correlationId, typeof(SomeCommand), command, [], new());

        var validator = new CancellingValidator();
        _discoverableValidators.TryGet(typeof(SomeCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _filter.OnExecution(_context));

    [Fact] void should_propagate_the_cancellation_instead_of_treating_it_as_invalid() => (_exception is OperationCanceledException).ShouldBeTrue();

    record SomeCommand(string Value);

    class CancellingValidator : AbstractValidator<SomeCommand>
    {
        public CancellingValidator() => RuleFor(c => c.Value).Must(_ => throw new OperationCanceledException());
    }
}
