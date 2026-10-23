// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_provisioning_decision_reads;

public class with_a_unique_constraint : Specification
{
    CommandScenario<StartProtectedOnboarding> _scenario;
    CommandResult _result;
    EventSourceId _existingPartner;
    EventSourceId _newPartner;

    void Establish()
    {
        _existingPartner = EventSourceId.New();
        _newPartner = EventSourceId.New();
        _scenario = new CommandScenario<StartProtectedOnboarding>();
        _scenario.Given.ForEventSource(_existingPartner).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new StartProtectedOnboarding(_newPartner, "ORG-123"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_constraint_violation() => _result.ValidationResults.Any(_ => _.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] async Task should_append_no_events_for_the_rejected_partner() => (await _scenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record StartProtectedOnboarding(EventSourceId EventSourceId, string OrganizationNumber)
    {
        public PartnerOnboardingStarted Handle() => new(OrganizationNumber);
    }
}
