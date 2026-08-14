// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_returning_events_with_concurrency_scopes;

public class and_a_constraint_rejects_one_event : Specification
{
    CommandScenario<StartPartnerOnboardingWithScopedBatch> _scenario;
    CommandResult _result;
    EventSourceId _existingPartner;
    EventSourceId _newPartner;
    EventSourceId _invitation;

    async Task Establish()
    {
        _existingPartner = EventSourceId.New();
        _newPartner = EventSourceId.New();
        _invitation = EventSourceId.New();
        _scenario = new();
        await _scenario.EventScenario.Given.ForEventSource(_existingPartner).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new(_newPartner, _invitation, "ORG-123"));

    [Fact] void should_fail_as_an_ordinary_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_constraint_violation() => _result.ValidationResults.Any(_ => _.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] async Task should_leave_no_rejected_event_residue() => (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();
    [Fact] async Task should_leave_no_sibling_event_residue() => (await _scenario.EventScenario.EventLog.HasEventsFor(_invitation)).ShouldBeFalse();
}
