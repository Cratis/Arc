// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class without_decision_mode : given.a_concurrent_append_scenario
{
    void Establish() => _scenario.AppendConcurrently(_source, new FactRecorded("competitor"));
    async Task Because() => _result = await _scenario.Execute(new AppendWithExplicitScope(_source));

    [Fact] void should_reject_the_owner_commit() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_not_capture_the_competitor_as_a_command_event() => _scenario.AppendedEvents.Select(_ => _.Event.Content).ShouldNotContain(new FactRecorded("competitor"));
    [Fact] void should_not_capture_successful_owner_events() => _scenario.AppendedEvents.Where(_ => _.Result.IsSuccess).ShouldBeEmpty();
    [Fact] async Task should_keep_the_competitor_in_the_existing_log() => (await _scenario.EventScenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Select(_ => _.Content).ShouldContainOnly(new FactRecorded("competitor"));
}
