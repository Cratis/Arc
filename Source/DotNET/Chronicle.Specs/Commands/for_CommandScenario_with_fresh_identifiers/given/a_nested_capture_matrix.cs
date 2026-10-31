// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.nested_freshness_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_nested_capture_matrix : Specification
{
    protected readonly List<Outcome> _outcomes = [];

    protected async Task ExecuteAll(bool occupied = false, bool explicitScopes = false)
    {
        foreach (var style in explicitScopes ? new[] { NestedStyle.ExplicitScope, NestedStyle.DirectScope } : [NestedStyle.StreamType, NestedStyle.StreamId, NestedStyle.SourceType, NestedStyle.Aggregate])
        {
            await using var scenario = new CommandScenario<CreateNestedSource>();
            var id = Identifier.New();
            scenario.PinGeneratedValue("id", id);
            if (occupied)
            {
                scenario.Given.ForEventSource(id).Events(new Created("existing"));
            }
            var before = await scenario.EventLog.GetTailSequenceNumber();
            var command = new CreateNestedSource(style);
            var result = await scenario.Execute(command);
            var after = await scenario.EventLog.GetTailSequenceNumber();
            _outcomes.Add(new(style, result, command.NestedResult, before, after, command.Calls));
        }
    }

    protected record Outcome(NestedStyle Style, CommandResult Result, CommandResult NestedResult, EventSequenceNumber Before, EventSequenceNumber After, int Calls);
}
