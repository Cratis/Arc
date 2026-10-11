// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_concept_validator_warns : given.a_generated_value_resolver
{
    void Establish() => WithValidator(new WarnsAboutId());
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_allow_the_generated_value() => _resolution.ControlResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_capture_the_value() => _context.GeneratedValues.Count.ShouldEqual(1);
    [Fact] void should_not_treat_it_as_request_validation() => _resolution.ControlResult.ValidationResults.ShouldBeEmpty();

    class WarnsAboutId : ConceptValidator<Id>
    {
        public WarnsAboutId() => RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(false)).WithSeverity(Severity.Warning);
    }
}
