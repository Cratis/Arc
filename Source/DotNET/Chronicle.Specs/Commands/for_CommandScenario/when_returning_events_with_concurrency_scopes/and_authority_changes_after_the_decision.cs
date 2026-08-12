// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_returning_events_with_concurrency_scopes;

public class and_authority_changes_after_the_decision : Specification
{
    CommandScenario<AuthorizeWithCapturedScope> _scenario;
    CommandResult _result;
    EventSourceId _authoritySource;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    EventSourceId _interferenceSource;
    EventSourceId _authorityScopeLabel;

    async Task Establish()
    {
        _authoritySource = EventSourceId.New();
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
        _interferenceSource = EventSourceId.New();
        _authorityScopeLabel = EventSourceId.New();
        _scenario = new();
        await _scenario.EventScenario.Given.ForEventSource(_authoritySource).Events(new AuthorityRevisionAdvanced());
    }

    async Task Because() => _result = await _scenario.Execute(new(
        _firstTarget,
        _secondTarget,
        _interferenceSource,
        _authorityScopeLabel,
        IncludeExactScope: true));

    [Fact] void should_fail_as_a_concurrency_rejection() => _result.ValidationResults.Any(_ => _.Reason == ValidationResultReason.ConcurrencyViolation).ShouldBeTrue();
    [Fact] async Task should_leave_no_first_target_residue() => (await _scenario.EventScenario.EventLog.HasEventsFor(_firstTarget)).ShouldBeFalse();
    [Fact] async Task should_leave_no_second_target_residue() => (await _scenario.EventScenario.EventLog.HasEventsFor(_secondTarget)).ShouldBeFalse();
    [Fact] async Task should_keep_the_deliberate_interference() => (await _scenario.EventScenario.EventLog.HasEventsFor(_interferenceSource)).ShouldBeTrue();
}
