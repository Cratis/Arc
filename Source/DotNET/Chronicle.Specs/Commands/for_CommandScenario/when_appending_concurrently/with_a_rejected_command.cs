// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class with_a_rejected_command : given.a_concurrent_append_scenario
{
    void Establish() => _scenario.AppendConcurrently(_source, new FactRecorded("competitor"));
    async Task Because() => _result = await _scenario.Execute(new AppendWithExplicitScope(_source, Reject: true));

    [Fact] void should_reject() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_run_competing_appends_without_a_successful_decision() => (await _scenario.EventLog.HasEventsFor(_source)).ShouldBeFalse();
}
