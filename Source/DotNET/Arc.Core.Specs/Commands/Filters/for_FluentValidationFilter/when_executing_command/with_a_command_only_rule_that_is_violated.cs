// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_a_command_only_rule_that_is_violated : given.a_fluent_validation_filter
{
    const string Message = "A command needs a name.";
    CommandResult _result;

    void Establish()
    {
        _discoverableValidators.TryGet(typeof(SomeCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = new SomeCommandValidator();
                return true;
            });
        _context = new CommandContext(_correlationId, typeof(SomeCommand), new SomeCommand(string.Empty), [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_rule_message() => _result.ValidationResults.Single().Message.ShouldEqual(Message);
    [Fact] void should_not_report_the_validator_failing() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.Rule);

    record SomeCommand(string Name);

    class SomeCommandValidator : BaseValidator<SomeCommand>
    {
        public SomeCommandValidator() => WhenCommand(() => RuleFor(x => x.Name).NotEmpty().WithMessage(Message));
    }
}
