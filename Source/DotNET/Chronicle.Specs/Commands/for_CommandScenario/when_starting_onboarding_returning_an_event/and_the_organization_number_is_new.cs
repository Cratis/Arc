// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_starting_onboarding_returning_an_event;

public class and_the_organization_number_is_new : Specification
{
    CommandScenario<StartPartnerOnboarding> _scenario;
    CommandResult _result;
    EventSourceId _partner;

    void Establish()
    {
        _partner = EventSourceId.New();
        _scenario = new CommandScenario<StartPartnerOnboarding>();
    }

    async Task Because() => _result = await _scenario.Execute(new StartPartnerOnboarding(_partner, "ORG-999"));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_commit_the_event_to_the_event_log() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();
}
