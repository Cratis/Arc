// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_onboarding;

public class and_onboarding_was_already_completed_for_a_different_partner : Specification
{
    CommandScenario<CompletePartnerOnboarding> _scenario;
    CommandResult _result;
    EventSourceId _otherPartner;
    EventSourceId _partner;

    async Task Establish()
    {
        _otherPartner = EventSourceId.New();
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<CompletePartnerOnboarding>();
        await _scenario.EventScenario.Given.ForEventSource(_otherPartner).Events(new PartnerOnboardingCompleted());
    }

    async Task Because() => _result = await _scenario.Execute(new CompletePartnerOnboarding(_partner));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_commit_the_event_to_the_event_log() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();
}
