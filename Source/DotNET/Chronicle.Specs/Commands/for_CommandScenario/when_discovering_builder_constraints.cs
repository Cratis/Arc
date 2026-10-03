// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_discovering_builder_constraints : Specification
{
    CommandScenario<StartPartnerOnboardingDirect> _scenario;
    EventSourceId _source;
    AppendResult _result;

    async Task Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New()).Events(new PartnerOnboardingReserved("ORG-123"));
    }

    async Task Because() => _result = await _scenario.EventLog.Append(_source, new PartnerOnboardingReserved("ORG-123"));

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_reject_the_duplicate() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_name_the_discovered_constraint() => _result.ConstraintViolations.Single().ConstraintName.Value.ShouldEqual(nameof(UniquePartnerOnboardingReservation));
    [Fact] async Task should_not_store_the_rejected_event() => (await _scenario.EventLog.HasEventsFor(_source)).ShouldBeFalse();
}
