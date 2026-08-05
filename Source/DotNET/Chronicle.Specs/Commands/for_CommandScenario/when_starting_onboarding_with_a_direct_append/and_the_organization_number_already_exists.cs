// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_starting_onboarding_with_a_direct_append;

public class and_the_organization_number_already_exists : Specification
{
    CommandScenario<StartPartnerOnboardingDirect> _scenario;
    CommandResult _result;
    EventSourceId _existingPartner;
    EventSourceId _newPartner;

    async Task Establish()
    {
        _existingPartner = EventSourceId.New();
        _newPartner = EventSourceId.New();
        _scenario = new CommandScenario<StartPartnerOnboardingDirect>();
        await _scenario.EventScenario.Given.ForEventSource(_existingPartner).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new StartPartnerOnboardingDirect(_newPartner, "ORG-123"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_constraint_violation() => _result.ValidationResults.Any(validationResult => validationResult.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] async Task should_append_no_events_for_the_rejected_partner() => (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();
}
