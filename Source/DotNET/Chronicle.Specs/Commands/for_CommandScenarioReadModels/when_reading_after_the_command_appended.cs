// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels;

public class when_reading_after_the_command_appended : Specification
{
    readonly EventSourceId _source = EventSourceId.New();
    CommandScenario<ReviseNote> _scenario;

    void Establish()
    {
        _scenario = new CommandScenario<ReviseNote>();
        _scenario.Given.ForEventSource(_source).Events(new NoteWritten("seed"));
    }

    async Task Because()
    {
        (await _scenario.Execute(new ReviseNote(_source, "one"))).ShouldBeSuccessful();
        (await _scenario.Execute(new ReviseNote(_source, "two"))).ShouldBeSuccessful();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] Task should_fold_the_seeded_note_for_the_first_execution() =>
        _scenario.ShouldHaveAppendedEvent<ReviseNote, NoteWritten>(_source, _ => _.Text == "seed>one");

    [Fact] void should_fold_the_first_append_for_the_second_execution() =>
        _scenario.AppendedEvents.Select(_ => ((NoteWritten)_.Event.Content).Text).ShouldEqual(["seed>one", "seed>one>two"]);
}
