// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

public class when_a_base_validator_rejects : given.a_validator_invoker
{
    class BaseSubjectValidator : BaseValidator<Subject>
    {
        public BaseSubjectValidator() => RuleFor(subject => subject.Name).NotEmpty().WithMessage("Name is required");
    }

    IEnumerable<ValidationResult> _results;

    async Task Because() => _results = await _invoker.Invoke(new Subject(string.Empty, "valid"), new BaseSubjectValidator(), string.Empty);

    [Fact] void should_report_the_authored_failure() => _results.Single().Message.ShouldEqual("Name is required");
    [Fact] void should_attribute_the_failure_to_its_member() => _results.Single().Members.ShouldContain("name");
}
