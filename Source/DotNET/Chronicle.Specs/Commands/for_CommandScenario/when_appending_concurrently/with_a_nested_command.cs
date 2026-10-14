// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class with_a_nested_command : given.a_concurrent_append_scenario
{
    readonly EventSourceId _nested = EventSourceId.New();

    void Establish() => _scenario.AppendConcurrently(_source, new FactRecorded("competitor"));
    async Task Because() => _result = await _scenario.Execute(new AppendWithExplicitScope(_source, _nested));

    [Fact] void should_append_the_competitor_after_the_outer_read() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] async Task should_roll_back_the_nested_event() => (await _scenario.EventLog.HasEventsFor(_nested)).ShouldBeFalse();
    [Fact] void should_not_capture_any_owner_events() => _scenario.AppendedEvents.Where(_ => _.Result.IsSuccess).ShouldBeEmpty();
}
