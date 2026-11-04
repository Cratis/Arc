// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Holds the list-built generated destinations emitted by Stage's V15 vector.
/// </summary>
public static class GeneratedListSources
{
    /// <summary>
    /// Wraps the Stage-shaped handler, with an optional unproven list, guard or scope use.
    /// </summary>
    /// <param name="variant">The behavior to exercise.</param>
    /// <returns>The source containing the handler and its generated concepts.</returns>
    public static string With(string variant = "supported") => """
        global using System.Collections.Generic;
        global using System.Linq;
        global using Cratis.Arc.Commands;
        global using Cratis.Chronicle.EventSequences;
        global using Cratis.Chronicle.EventSequences.Concurrency;
        """ + GeneratedValueSources.With($$"""
        public record Receipt(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value), Cratis.Concepts.IGeneratable<Receipt>
        {
            public static Receipt New() => new(Guid.NewGuid());
        }
        [EventType] public record Started(Receipt Receipt);
        [EventType] public record Initialized();
        [Command] public record Create(bool Enabled{{(variant == "unsupported-equals" ? ", object Other" : "")}})
        {
            static void Escape(List<EventForEventSourceId> events) { }
            static void Mutate(ref bool enabled) { enabled = !enabled; }
            public (Receipt, EventsWithConcurrencyScopes) Handle(
                [GeneratedValue("id", Freshness = GeneratedValueFreshness.Unguarded)] AuthorId id,
                [GeneratedValue("receipt", Freshness = GeneratedValueFreshness.Unguarded)] Receipt receipt)
            {
                {{(variant == "side-effect" ? "var changed = Console.ReadLine();" : "")}}
                var initial = Enabled == true;
                var stageProduces0 = {{Guard(variant)}};
                var stageProduces1 = {{Guard(variant)}};
                {{(variant == "reassigned" ? "stageProduces0 = !stageProduces0;" : "")}}
                {{(variant == "ref" ? "Mutate(ref stageProduces0);" : "")}}
                var stageEvents = new List<EventForEventSourceId>();
                {{(variant == "escape" ? "Escape(stageEvents);" : "")}}
                {{(variant == "loop" ? "while (Enabled)" : "if (stageProduces0)")}}
                {
                    stageEvents.Add(new EventForEventSourceId({{(variant == "computed" ? "AuthorId.New()" : "id")}}, new Started(receipt)));
                }
                if (stageProduces1)
                {
                    stageEvents.Add(new EventForEventSourceId(id, new Initialized()));
                }
                return (receipt, new EventsWithConcurrencyScopes(stageEvents, Enumerable.Select(Enumerable.Distinct<EventSourceId>([.. (stageProduces0 || stageProduces1 ? new EventSourceId[] { {{(variant == "missing" ? "AuthorId.New()" : "id")}} } : [])]), source => new KeyValuePair<EventSourceId, ConcurrencyScope>(source, new({{(variant == "sequence" ? "new EventSequenceNumber(1)" : "EventSequenceNumber.BeforeFirst")}}, EventSourceId: source)))));
            }
        }
        """);

    static string Guard(string variant) => variant switch
    {
        "alias" => "initial",
        "unqualified-equals" => "Equals(Enabled, true)",
        "instance-equals" => "Enabled.Equals(true)",
        "unsupported-equals" => "object.Equals(Other, Other)",
        "non-command-equals" => "object.Equals(DateTime.UtcNow.Ticks > 0, true)",
        _ => "(object.Equals(Enabled, true))"
    };
}
