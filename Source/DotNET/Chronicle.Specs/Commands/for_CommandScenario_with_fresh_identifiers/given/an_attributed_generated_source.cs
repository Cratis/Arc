// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class an_attributed_generated_source<TCommand> : Specification
    where TCommand : class, attributed_source_commands.ICreateSource, new()
{
    protected CommandScenario<TCommand> _scenario;
    protected TCommand _command;
    protected CommandResult _result;
    protected EventSequenceNumber _tailBefore;
    protected EventSequenceNumber _tailAfter;
    a_fresh_identifier_scenario.Identifier _id;

    void Establish()
    {
        _id = new(new Guid(0xa21121b7, 0xfa8a, 0x4f8c, 0xb2, 0xba, 0x19, 0xd2, 0xaf, 0xd8, 0x13, 0xf7));
        _command = new();
        _scenario = new CommandScenario<TCommand>().UseDecisionReads();
        _scenario.PinGeneratedValue("id", _id);
    }

    protected async Task SeedOccupiedSource()
    {
        _scenario.Given.ForEventSource(_id).Events(new a_fresh_identifier_scenario.Created("existing"));
        _tailBefore = await _scenario.EventLog.GetTailSequenceNumber();
    }

    protected async Task Execute()
    {
        _result = await _scenario.Execute(_command);
        _tailAfter = await _scenario.EventLog.GetTailSequenceNumber();
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
