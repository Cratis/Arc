// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_a_command_only_rule_that_is_satisfied : given.a_fluent_validation_filter
{
    CommandResult _result;

    void Establish()
    {
        _discoverableValidators.TryGet(typeof(SomeCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = new SomeCommandValidator();
                return true;
            });
        _context = new CommandContext(_correlationId, typeof(SomeCommand), new SomeCommand("a name"), [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.ValidationResults.ShouldBeEmpty();

    record SomeCommand(string Name);

    class SomeCommandValidator : BaseValidator<SomeCommand>
    {
        public SomeCommandValidator() => WhenCommand(() => RuleFor(x => x.Name).NotEmpty());
    }
}
