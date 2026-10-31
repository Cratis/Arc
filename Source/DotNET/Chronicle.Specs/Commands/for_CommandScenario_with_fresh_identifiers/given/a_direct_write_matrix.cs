// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_direct_write_matrix : Specification
{
    protected readonly Identifier _id = new(new Guid(0xe8fb3272, 0x75d4, 0x4d04, 0x96, 0x02, 0x14, 0xe7, 0x5a, 0x05, 0x8e, 0x14));
    protected readonly List<Outcome> _outcomes = [];

    protected async Task ExecuteAll(bool occupied = false, bool conflict = false, bool unrelated = false, bool equalScope = false, bool decisionReads = true)
    {
        foreach (var style in Enum.GetValues<WriteStyle>())
        {
            await using var scenario = new CommandScenario<DirectWrite>();
            if (decisionReads)
            {
#pragma warning disable CS0618 // This matrix deliberately covers the legacy explicit switch.
                scenario.UseDecisionReads();
#pragma warning restore CS0618
            }
            scenario.PinGeneratedValue("id", _id);
            if (occupied)
            {
                scenario.Given.ForEventSource(_id).Events(new Created("existing"));
            }
            var before = await scenario.EventLog.GetTailSequenceNumber();
            var command = new DirectWrite(style, conflict, unrelated, equalScope);
            var result = await scenario.Execute(command);
            var after = await scenario.EventLog.GetTailSequenceNumber();
            _outcomes.Add(new(result, before, after, command.Calls));
        }
    }

    protected record Outcome(CommandResult Result, EventSequenceNumber Before, EventSequenceNumber After, int Calls);
}
