// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_default_mode_scenario : Specification
{
    protected CommandScenario<CreateSource> _scenario;
    protected Identifier _id;
    protected CreateSource _command;
    protected CommandResult _result;
    protected Exception? _error;

    void Establish()
    {
        _id = new(new Guid(0xfb6dd43a, 0xb161, 0x44b1, 0x9b, 0xaa, 0x7b, 0x2e, 0x4a, 0x07, 0x16, 0x80));
        _scenario = new CommandScenario<CreateSource>().PinGeneratedValue("id", _id);
        _command = new(ResultShape.Wrapper);
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command]
    public record CreateUnguardedSource
    {
        public EventForEventSourceId Handle([GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Identifier id) =>
            new(id, new Created("first"));
    }

    [Command]
    public record CreateNamedSource
    {
        public int Calls { get; private set; }

        public EventForEventSourceId Handle([GeneratedValue("sourceId")] Identifier id)
        {
            Calls++;

            return new(id, new Created("first"));
        }
    }
}
