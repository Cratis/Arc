// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_via_the_event_store;

public class and_the_organization_number_already_exists : Specification
{
    CommandScenario<AppendViaEventStoreWithDuplicate> _scenario;
    CommandResult _result;
    EventSourceId _partner;

    async Task Establish()
    {
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<AppendViaEventStoreWithDuplicate>();
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New()).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new AppendViaEventStoreWithDuplicate(_partner, "ORG-123"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_constraint_violation() => _result.ValidationResults.Any(validationResult => validationResult.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] async Task should_append_nothing_for_the_rejected_partner() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeFalse();
}
