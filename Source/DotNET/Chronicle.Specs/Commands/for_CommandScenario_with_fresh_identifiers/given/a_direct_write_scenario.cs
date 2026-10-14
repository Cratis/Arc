// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_direct_write_scenario<TCommand> : Specification
    where TCommand : class
{
    protected CommandScenario<TCommand> _scenario;
    protected Identifier _id;
    protected CommandResult _result;
    protected EventSequenceNumber _before;
    protected EventSequenceNumber _after;

    void Establish()
    {
        _id = Identifier.New();
        _scenario = new CommandScenario<TCommand>().UseDecisionReads();
        _scenario.PinGeneratedValue("id", _id);
    }

    protected void SeedOccupiedSource() => _scenario.Given.ForEventSource(_id).Events(new Created("existing"));

    protected async Task Execute(TCommand command)
    {
        _before = await _scenario.EventLog.GetTailSequenceNumber();
        _result = await _scenario.Execute(command);
        _after = await _scenario.EventLog.GetTailSequenceNumber();
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
