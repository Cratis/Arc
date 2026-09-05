// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_starting_onboarding_returning_an_event;

public class and_the_organization_number_already_exists : Specification
{
    CommandScenario<StartPartnerOnboarding> _scenario;
    CommandResult _result;
    EventSourceId _existingPartner;
    EventSourceId _newPartner;

    async Task Establish()
    {
        _existingPartner = EventSourceId.New();
        _newPartner = EventSourceId.New();
        _scenario = new CommandScenario<StartPartnerOnboarding>();
        await _scenario.EventScenario.Given.ForEventSource(_existingPartner).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new StartPartnerOnboarding(_newPartner, "ORG-123"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
}
