// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_concept_validator_accepts_it : given.a_generated_value_resolver
{
    void Establish() => WithValidator(new AcceptsId());
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_allow_the_generated_value() => _resolution.ControlResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_capture_the_value() => _context.GeneratedValues.Get<Id>("id").ShouldEqual(_resolution.Arguments.Single());
    [Fact] void should_not_report_validation_failures() => _resolution.ControlResult.ValidationResults.ShouldBeEmpty();

    class AcceptsId : ConceptValidator<Id>
    {
        public AcceptsId()
        {
            RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(true));
            RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(true)).WithSeverity(Severity.Warning);
            RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(true)).WithSeverity(Severity.Info);
        }
    }
}
