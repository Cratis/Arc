// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels;

public class when_two_scenarios_read_the_same_event_source : Specification
{
    readonly EventSourceId _source = EventSourceId.New();
    CommandScenario<ReportTally> _first;
    CommandScenario<ReportTally> _second;

    void Establish()
    {
        _first = new CommandScenario<ReportTally>();
        _first.Given.ForEventSource(_source).Events(new NoteWritten("first"), new TallyRaised(1));
        _second = new CommandScenario<ReportTally>();
        _second.Given.ForEventSource(_source).Events(new NoteWritten("second"), new TallyRaised(2), new TallyRaised(3));
    }

    async Task Because()
    {
        (await _first.Execute(new ReportTally(_source))).ShouldBeSuccessful();
        (await _second.Execute(new ReportTally(_source))).ShouldBeSuccessful();
    }

    async Task Destroy()
    {
        await _first.DisposeAsync();
        await _second.DisposeAsync();
    }

    [Fact] Task should_fold_the_first_scenario_from_its_own_events() =>
        _first.ShouldHaveAppendedEvent<ReportTally, Reported>(_source, _ => _.Note == "first" && _.Total == 1);

    [Fact] Task should_fold_the_second_scenario_from_its_own_events() =>
        _second.ShouldHaveAppendedEvent<ReportTally, Reported>(_source, _ => _.Note == "second" && _.Total == 5);
}
