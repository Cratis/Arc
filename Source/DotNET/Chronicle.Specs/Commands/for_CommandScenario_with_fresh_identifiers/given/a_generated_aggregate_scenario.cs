// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_generated_aggregate_scenario : Specification
{
    protected CommandScenario<CreateAggregate> _scenario;
    protected a_fresh_identifier_scenario.Identifier _id;
    protected CreateAggregate _command;
    protected CommandResult _result;

    void Establish()
    {
        _id = new(new Guid(0xcf055c76, 0x77e0, 0x4389, 0x98, 0x4c, 0xab, 0x87, 0x00, 0xe3, 0x3e, 0x0a));
        _scenario = new CommandScenario<CreateAggregate>().UseDecisionReads();
        _scenario.PinGeneratedValue("id", _id);
        _command = new(false);
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    public class NewAggregate : AggregateRoot
    {
        public int Entries { get; private set; }

        void On(a_fresh_identifier_scenario.Created @event) => Entries++;
    }

    [Command]
    public record CreateAggregate(bool CompetingWriter)
    {
        public int Calls { get; private set; }

        public async Task<AggregateRootCommitResult> Handle([GeneratedValue] a_fresh_identifier_scenario.Identifier id, IAggregateRootFactory factory, IEventLog eventLog)
        {
            Calls++;
            var aggregate = await factory.Get<NewAggregate>(id);
            if (CompetingWriter)
            {
                var competing = await eventLog.Append(id, new a_fresh_identifier_scenario.Created("competing"), concurrencyScope: ConcurrencyScope.None);
                if (!competing.IsSuccess)
                {
                    return AggregateRootCommitResult.WithErrors([ValidationResult.Error("The spec's competing writer could not append.")]);
                }
            }
            await aggregate.Apply(new a_fresh_identifier_scenario.Created("first"));
            await aggregate.Apply(new a_fresh_identifier_scenario.Created("second"));

            return await aggregate.Commit();
        }
    }
}
