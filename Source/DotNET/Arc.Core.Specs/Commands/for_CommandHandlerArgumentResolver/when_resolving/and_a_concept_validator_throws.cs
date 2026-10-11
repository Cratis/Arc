// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_concept_validator_throws : given.a_generated_value_resolver
{
    Exception _error;

    void Establish() => WithValidator(new ThrowsForId());
    async Task Because() => _error = await Catch.Exception(async () => await Resolve());

    [Fact] void should_report_a_server_defect() => _error.ShouldBeOfExactType<GeneratedValueRejected>();
    [Fact] void should_not_expose_the_thrown_message() => _error.Message.ShouldNotContain("sensitive");
    [Fact] void should_keep_a_safe_failure_description() => ((GeneratedValueRejected)_error).Failures.ShouldContainOnly(ValidatorInvoker.CouldNotValidateMessage);
    [Fact] void should_not_capture_the_value() => _context.GeneratedValues.ShouldBeEmpty();

    class ThrowsForId : ConceptValidator<Id>
    {
        public ThrowsForId() => RuleFor(id => id.Value).MustAsync((value, _) => throw new BrokenGeneratorValidation(value));
    }

    class BrokenGeneratorValidation(Guid value) : Exception($"sensitive {value}");
}
