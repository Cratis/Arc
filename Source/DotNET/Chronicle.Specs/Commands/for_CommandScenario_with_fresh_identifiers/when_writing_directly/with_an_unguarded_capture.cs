// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_an_unguarded_capture : Specification
{
    readonly List<CommandResult> _results = [];
    readonly List<bool> _appended = [];

    async Task Because()
    {
        foreach (var style in Enum.GetValues<WriteStyle>())
        {
#pragma warning disable CS0618 // Framework compatibility: direct writes with the legacy single-log opt-in.
            await using var scenario = new CommandScenario<UnguardedDirectWrite>().UseDecisionReads();
#pragma warning restore CS0618
            var id = Identifier.New();
            scenario.PinGeneratedValue("id", id);
            scenario.Given.ForEventSource(id).Events(new Created("existing"));
            var before = await scenario.EventLog.GetTailSequenceNumber();
            _results.Add(await scenario.Execute(new(style)));
            _appended.Add(await scenario.EventLog.GetTailSequenceNumber() == before + 2);
        }
    }

    [Fact] void should_exercise_all_seven_apis() => _results.Count.ShouldEqual(7);
    [Fact] void should_allow_appending_to_existing_sources() => _results.ShouldEachConformTo(result => result.IsSuccess);
    [Fact] void should_append_every_event() => _appended.ShouldEachConformTo(appended => appended);
}
