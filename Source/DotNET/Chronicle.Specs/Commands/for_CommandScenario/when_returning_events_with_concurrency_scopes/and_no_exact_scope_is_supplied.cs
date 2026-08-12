// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_returning_events_with_concurrency_scopes;

public class and_no_exact_scope_is_supplied : Specification
{
    CommandScenario<AuthorizeWithCapturedScope> _scenario;
    CommandResult _result;
    EventSourceId _authoritySource;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    EventSourceId _interferenceSource;

    async Task Establish()
    {
        _authoritySource = EventSourceId.New();
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
        _interferenceSource = EventSourceId.New();
        _scenario = new();
        await _scenario.EventScenario.Given.ForEventSource(_authoritySource).Events(new AuthorityRevisionAdvanced());
    }

    async Task Because() => _result = await _scenario.Execute(new(
        _firstTarget,
        _secondTarget,
        _interferenceSource,
        EventSourceId.New(),
        IncludeExactScope: false));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_append_the_first_target_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_firstTarget)).ShouldBeTrue();
    [Fact] async Task should_append_the_second_target_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_secondTarget)).ShouldBeTrue();
}
