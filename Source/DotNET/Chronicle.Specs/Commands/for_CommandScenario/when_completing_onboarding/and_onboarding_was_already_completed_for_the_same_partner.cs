// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_onboarding;

public class and_onboarding_was_already_completed_for_the_same_partner : Specification
{
    CommandScenario<CompletePartnerOnboarding> _scenario;
    CommandResult _result;
    EventSourceId _partner;

    async Task Establish()
    {
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<CompletePartnerOnboarding>();
        await _scenario.EventScenario.Given.ForEventSource(_partner).Events(new PartnerOnboardingCompleted());
    }

    async Task Because() => _result = await _scenario.Execute(new CompletePartnerOnboarding(_partner));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
    [Fact] void should_surface_the_constraint_violation() => _result.ValidationResults.Any(validationResult => validationResult.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] void should_name_the_violated_constraint() => _result.ValidationResults.Single(validationResult => validationResult.Reason == ValidationResultReason.ConstraintViolation).ReasonDetail.ShouldEqual("UniqueOnboardingCompletionPerPartner");
}
