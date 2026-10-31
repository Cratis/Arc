// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public class a_fresh_identifier_scenario : Specification
{
    protected CommandScenario<CreateSource> _scenario;
    protected Identifier _id;
    protected CreateSource _command;
    protected CommandResult _result;
    protected EventSequenceNumber _tailBefore;
    protected EventSequenceNumber _tailAfter;

    void Establish()
    {
        _id = new(new Guid(0xfb6dd43a, 0xb161, 0x44b1, 0x9b, 0xaa, 0x7b, 0x2e, 0x4a, 0x07, 0x16, 0x80));
        _scenario = new CommandScenario<CreateSource>();
        _scenario.PinGeneratedValue("id", _id);
        _command = new(ResultShape.Wrapper);
    }

    protected async Task SeedOccupiedSource()
    {
        _scenario.Given.ForEventSource(_id).Events(new Created("existing"));
        _tailBefore = await _scenario.EventLog.GetTailSequenceNumber();
    }

    protected async Task Execute()
    {
        _result = await _scenario.Execute(_command);
        _tailAfter = await _scenario.EventLog.GetTailSequenceNumber();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    public enum ResultShape
    {
        Wrapper = 0,
        Wrappers = 1,
        Mixed = 2,
        Scoped = 3,
        Tuple = 4,
        ReverseTuple = 5,
        PlainEvents = 6
    }

    public record Identifier(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<Identifier>
    {
        public static Identifier New() => new(GenerateValue.Uuid());
    }

    [EventType("Arc.Chronicle.Specs.FreshSourceRecorded")]
    public record Created(string Name);

    [Command]
    public record CreateSource(ResultShape Shape)
    {
        public int Calls { get; private set; }

        public object Handle([GeneratedValue] Identifier id)
        {
            Calls++;
            var first = new Created("first");
            var second = new Created("second");
            var wrapper = new EventForEventSourceId(id, first);

            return Shape switch
            {
                ResultShape.Wrappers => new[] { wrapper, new EventForEventSourceId(id, second) },
                ResultShape.Mixed => (id, new object[] { first, new EventForEventSourceId(id, second) }),
                ResultShape.Scoped => new EventsWithConcurrencyScopes([wrapper, new(id, second)], [new KeyValuePair<EventSourceId, ConcurrencyScope>(id, new(EventSequenceNumber.BeforeFirst, EventSourceId: id))]),
                ResultShape.Tuple => (id, first),
                ResultShape.ReverseTuple => (first, id),
                ResultShape.PlainEvents => (id, new[] { first, second }),
                _ => wrapper
            };
        }
    }
}
