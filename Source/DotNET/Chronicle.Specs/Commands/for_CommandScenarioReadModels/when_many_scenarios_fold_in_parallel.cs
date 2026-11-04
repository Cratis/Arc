// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels;

public class when_many_scenarios_fold_in_parallel : Specification
{
    const int ScenarioCount = 24;
    readonly EventSourceId _source = EventSourceId.New();
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => Task.WhenAll(Enumerable.Range(1, ScenarioCount).Select(index => Task.Run(() => index % 2 == 0 ? Legacy(index) : Decision(index)))));

    [Fact] void should_let_every_scenario_see_exactly_its_own_events() => _error.ShouldBeNull();

    async Task Legacy(int index)
    {
        await using var scenario = new CommandScenario<ReportTally>();
        scenario.Given.ForEventSource(_source).Events(new NoteWritten($"note-{index}"), new TallyRaised(index));
        (await scenario.Execute(new ReportTally(_source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportTally, Reported>(_source, _ => _.Note == $"note-{index}" && _.Total == index);
    }

    async Task Decision(int index)
    {
        await using var scenario = new CommandScenario<ReportUnderDecision>();
        scenario.Given.ForEventSource(_source).Events(new NoteWritten($"note-{index}"), new TallyRaised(index));
        (await scenario.Execute(new ReportUnderDecision(_source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReportUnderDecision, Reported>(_source, _ => _.Note == $"note-{index}" && _.Total == index);
    }
}
