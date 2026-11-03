// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels;

public class when_a_reducer_and_a_projection_read_model_are_read
{
    [Fact]
    public async Task should_fold_both_in_the_legacy_route()
    {
        var source = EventSourceId.New();
        await using var scenario = new CommandScenario<ReportTally>();
        scenario.Given.ForEventSource(source).Events(new TallyRaised(4), new NoteWritten("early"), new TallyRaised(6), new NoteWritten("late"));
        (await scenario.Execute(new ReportTally(source), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportTally, Reported>(source, _ => _.Note == "late" && _.Total == 10);
    }

    [Fact]
    public async Task should_fold_both_in_the_decision_route()
    {
        var source = EventSourceId.New();
        await using var scenario = new CommandScenario<ReportUnderDecision>();
        scenario.Given.ForEventSource(source).Events(new TallyRaised(4), new NoteWritten("early"), new TallyRaised(6), new NoteWritten("late"));
        (await scenario.Execute(new ReportUnderDecision(source), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportUnderDecision, Reported>(source, _ => _.Note == "late" && _.Total == 10);
    }
}
