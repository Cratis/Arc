// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_concept_validator_rejects_it : given.a_generated_value_resolver
{
    Exception _error;

    void Establish() => WithValidator(new RejectsId());
    async Task Because() => _error = await Catch.Exception(async () => await Resolve());

    [Fact] void should_report_a_server_defect() => _error.ShouldBeOfExactType<GeneratedValueRejected>();
    [Fact] void should_keep_validator_failures_separate() => ((GeneratedValueRejected)_error).Failures.Single().ShouldContain("sensitive");
    [Fact] void should_not_expose_the_validator_message() => _error.Message.ShouldNotContain("sensitive");
    [Fact] void should_not_capture_a_rejected_value() => _context.GeneratedValues.ShouldBeEmpty();

    class RejectsId : ConceptValidator<Id>
    {
        public RejectsId() => RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(false)).WithMessage("sensitive {PropertyValue}");
    }
}
