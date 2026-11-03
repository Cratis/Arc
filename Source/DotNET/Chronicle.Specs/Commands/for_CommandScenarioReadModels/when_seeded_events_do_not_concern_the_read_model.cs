// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels;

public class when_seeded_events_do_not_concern_the_read_model
{
    [Fact]
    public async Task should_leave_both_read_models_absent_in_the_legacy_route()
    {
        var source = EventSourceId.New();
        await using var scenario = new CommandScenario<ReportTally>();
        scenario.Given.ForEventSource(source).Events(new GuardChanged("unrelated"));
        (await scenario.Execute(new ReportTally(source), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportTally, Reported>(source, _ => _.Note == Reported.Absent && _.Total == Reported.NoTotal);
    }

    [Fact]
    public async Task should_leave_both_read_models_absent_in_the_decision_route()
    {
        var source = EventSourceId.New();
        await using var scenario = new CommandScenario<ReportUnderDecision>();
        scenario.Given.ForEventSource(source).Events(new GuardChanged("unrelated"));
        (await scenario.Execute(new ReportUnderDecision(source), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportUnderDecision, Reported>(source, _ => _.Note == Reported.Absent && _.Total == Reported.NoTotal);
    }
}
