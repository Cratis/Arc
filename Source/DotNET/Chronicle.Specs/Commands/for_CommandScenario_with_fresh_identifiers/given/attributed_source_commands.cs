// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public static class attributed_source_commands
{
    public interface ICreateSource
    {
        a_fresh_identifier_scenario.ResultShape Shape { get; set; }
        int Calls { get; }
    }

    [Command]
    [EventStreamType("named-stream", concurrency: true)]
    public record WithStreamType : ICreateSource
    {
        public a_fresh_identifier_scenario.ResultShape Shape { get; set; }
        public int Calls { get; private set; }
        public object Handle([GeneratedValue] a_fresh_identifier_scenario.Identifier id)
        {
            Calls++;
            return CreateEvents(id, Shape);
        }
    }

    [Command]
    [EventStreamId("named-instance", concurrency: true)]
    public record WithStreamId : ICreateSource
    {
        public a_fresh_identifier_scenario.ResultShape Shape { get; set; }
        public int Calls { get; private set; }
        public object Handle([GeneratedValue] a_fresh_identifier_scenario.Identifier id)
        {
            Calls++;
            return CreateEvents(id, Shape);
        }
    }

    [Command]
    [EventSourceType("named-source", concurrency: true)]
    public record WithSourceType : ICreateSource
    {
        public a_fresh_identifier_scenario.ResultShape Shape { get; set; }
        public int Calls { get; private set; }
        public object Handle([GeneratedValue] a_fresh_identifier_scenario.Identifier id)
        {
            Calls++;
            return CreateEvents(id, Shape);
        }
    }

    static object CreateEvents(a_fresh_identifier_scenario.Identifier id, a_fresh_identifier_scenario.ResultShape shape)
    {
        var first = new a_fresh_identifier_scenario.Created("first");
        var second = new a_fresh_identifier_scenario.Created("second");
        return shape switch
        {
            a_fresh_identifier_scenario.ResultShape.Tuple => (id, first),
            a_fresh_identifier_scenario.ResultShape.PlainEvents => (id, new[] { first, second }),
            a_fresh_identifier_scenario.ResultShape.Wrappers => new EventForEventSourceId[] { new(id, first), new(id, second) },
            _ => new EventForEventSourceId(id, first)
        };
    }
}
