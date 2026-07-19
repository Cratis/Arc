// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_mixing_append_styles;

public class and_the_immediate_append_is_rejected : Specification
{
    CommandScenario<AppendMixedStylesWithDuplicate> _scenario;
    CommandResult _result;
    EventSourceId _transactionalId;
    EventSourceId _immediateId;

    async Task Establish()
    {
        _transactionalId = EventSourceId.New();
        _immediateId = EventSourceId.New();
        _scenario = new CommandScenario<AppendMixedStylesWithDuplicate>();
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New()).Events(new PartnerOnboardingStarted("ORG-123"));
    }

    async Task Because() => _result = await _scenario.Execute(new AppendMixedStylesWithDuplicate(_transactionalId, _immediateId, "ORG-123"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_append_the_rejected_immediate_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_immediateId)).ShouldBeFalse();
    [Fact] async Task should_roll_back_the_transactional_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_transactionalId)).ShouldBeFalse();
}
