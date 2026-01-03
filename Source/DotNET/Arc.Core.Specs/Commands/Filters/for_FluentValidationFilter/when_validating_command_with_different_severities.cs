// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Filters.for_FluentValidationFilter.given;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter;

public class when_validating_command_with_different_severities : a_fluent_validation_filter
{
    record TestCommand(string Name, string Email, string Phone);

    class TestCommandValidator : BaseValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithSeverity(ValidationResultSeverity.Error);
            RuleFor(x => x.Email).NotEmpty().WithSeverity(ValidationResultSeverity.Warning);
            RuleFor(x => x.Phone).NotEmpty().WithSeverity(ValidationResultSeverity.Information);
        }
    }

    CommandResult _result;

    void Establish()
    {
        var command = new TestCommand(string.Empty, string.Empty, string.Empty);
        _context = new CommandContext(_correlationId, typeof(TestCommand), command, [], new());

        var validator = new TestCommandValidator();
        _discoverableValidators.TryGet(typeof(TestCommand), out Arg.Any<IValidator>()).Returns(call =>
        {
            call[1] = validator;
            return true;
        });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_three_validation_results() => _result.ValidationResults.Count().ShouldEqual(3);
    [Fact] void should_have_error_severity_for_name() => _result.ValidationResults.Single(v => v.Members.Any(m => m.Equals("Name", StringComparison.OrdinalIgnoreCase))).Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_have_warning_severity_for_email() => _result.ValidationResults.Single(v => v.Members.Any(m => m.Equals("Email", StringComparison.OrdinalIgnoreCase))).Severity.ShouldEqual(ValidationResultSeverity.Warning);
    [Fact] void should_have_information_severity_for_phone() => _result.ValidationResults.Single(v => v.Members.Any(m => m.Equals("Phone", StringComparison.OrdinalIgnoreCase))).Severity.ShouldEqual(ValidationResultSeverity.Information);
}
