// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_starting_onboarding_returning_events_for_multiple_sources;

public class and_the_organization_number_already_exists : Specification
{
    CommandScenario<StartPartnerOnboardingWithInvite> _scenario;
    CommandResult _result;
    EventSourceId _existingPartner;
    EventSourceId _newPartner;
    EventSourceId _invitationId;

    async Task Establish()
    {
        _existingPartner = EventSourceId.New();
        _newPartner = EventSourceId.New();
        _invitationId = EventSourceId.New();
        _scenario = new CommandScenario<StartPartnerOnboardingWithInvite>();
        await _scenario.EventScenario.Given.ForEventSource(_existingPartner).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new StartPartnerOnboardingWithInvite(_newPartner, _invitationId, "ORG-123"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_append_the_rejected_onboarding_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();
    [Fact] async Task should_not_append_the_sibling_invitation() => (await _scenario.EventScenario.EventLog.HasEventsFor(_invitationId)).ShouldBeFalse();
}
