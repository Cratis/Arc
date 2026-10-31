// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class two_generated_sources : Specification
{
    protected CommandScenario<CreateTwoSources> _scenario;
    protected a_fresh_identifier_scenario.Identifier _first;
    protected a_fresh_identifier_scenario.Identifier _second;
    protected CreateTwoSources _command;
    protected CommandResult _result;

    void Establish()
    {
        _first = new(new Guid(0x64aa36d7, 0xd559, 0x4f6d, 0x82, 0x74, 0x3d, 0x6b, 0x6e, 0x66, 0xa1, 0x9b));
        _second = new(new Guid(0x51a050b4, 0x97f7, 0x4a44, 0x91, 0xe1, 0x4d, 0x09, 0xdb, 0x94, 0x3b, 0x7b));
        _scenario = new CommandScenario<CreateTwoSources>();
        _scenario.PinGeneratedValue("first", _first);
        _scenario.PinGeneratedValue("second", _second);
        _command = new();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command]
    public record CreateTwoSources
    {
        public int Calls { get; private set; }

        public IEnumerable<EventForEventSourceId> Handle([GeneratedValue] a_fresh_identifier_scenario.Identifier first, [GeneratedValue] a_fresh_identifier_scenario.Identifier second)
        {
            Calls++;

            return [new(first, new a_fresh_identifier_scenario.Created("first")), new(second, new a_fresh_identifier_scenario.Created("second"))];
        }
    }
}
