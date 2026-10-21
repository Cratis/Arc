// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public static class nested_freshness_commands
{
    public enum NestedStyle
    {
        StreamType = 0,
        StreamId = 1,
        SourceType = 2,
        Aggregate = 3,
        ExplicitScope = 4,
        DirectScope = 5
    }

    [Command]
    public record CreateNestedSource(NestedStyle Style)
    {
        public int Calls { get; private set; }
        public CommandResult NestedResult { get; private set; } = null!;
        public async Task<object> Handle([GeneratedValue] Identifier id, ICommandPipeline pipeline)
        {
            Calls++;
            object command = Style switch
            {
                NestedStyle.StreamId => new WithStreamId(id),
                NestedStyle.SourceType => new WithSourceType(id),
                NestedStyle.Aggregate => new WithAggregate(id),
                NestedStyle.ExplicitScope => new WithExplicitScope(id),
                NestedStyle.DirectScope => new WithDirectScope(id),
                _ => new WithStreamType(id)
            };

            NestedResult = await pipeline.Execute(command);

            // Propagate the single nested rejection rather than treating CommandResult as an ordinary response.
            return NestedResult.IsSuccess ? true : NestedResult.ValidationResults.FirstOrDefault() ?? ValidationResult.Error(string.Join(Environment.NewLine, NestedResult.ExceptionMessages));
        }
    }

    [Command]
    [EventStreamType("named-stream", concurrency: true)]
    public record WithStreamType(Identifier Id)
    {
        public EventForEventSourceId Handle() => new(Id, new Created("nested"));
    }

    [Command]
    [EventStreamId("named-instance", concurrency: true)]
    public record WithStreamId(Identifier Id)
    {
        public EventForEventSourceId Handle() => new(Id, new Created("nested"));
    }

    [Command]
    [EventSourceType("named-source", concurrency: true)]
    public record WithSourceType(Identifier Id)
    {
        public EventForEventSourceId Handle() => new(Id, new Created("nested"));
    }

    [Command]
    public record WithAggregate(Identifier Id)
    {
        public async Task Handle(IAggregateRootFactory factory)
        {
            var aggregate = await factory.Get<a_generated_aggregate_scenario.NewAggregate>(Id);
            await aggregate.Apply(new Created("nested"));
        }
    }

    [Command]
    public record WithExplicitScope(Identifier Id)
    {
        public EventsWithConcurrencyScopes Handle() => new([new(Id, new Created("nested"))], [new KeyValuePair<EventSourceId, ConcurrencyScope>(Id, ConcurrencyScope.None)]);
    }

    [Command]
    public record WithDirectScope(Identifier Id)
    {
        public Task Handle(IEventLog log) => log.Transactional.Append(Id, new Created("nested"), concurrencyScope: ConcurrencyScope.None);
    }
}
