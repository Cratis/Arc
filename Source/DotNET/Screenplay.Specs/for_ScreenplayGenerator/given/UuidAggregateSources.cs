// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class UuidAggregateSources
{
    internal static (string Path, string Text)[] With(string arguments, string parameters = "Guid left, Guid right", string comparison = "left == right", string memberDefinition = "public record MemberId(Guid Value) : ConceptAs<Guid>(Value);") =>
    [
        (Analyzed.SlicePath, $$"""
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Lending.Reserving;
        [Command] public record ReserveBook([Cratis.Chronicle.Keys.Key] string Id, Guid Owner, Guid Other)
        {
            public MemberId Member { get; init; }
            public MemberId OtherMember { get; init; }
            public PlantId Plant { get; init; }
            public Guid? Maybe { get; init; }
            public async Task Handle(Reservation reservation)
            {
                await reservation.Reserve({{arguments}});
                await reservation.Commit();
            }
        }
        """),
        ("Library/Lending/Reserving/Reservation.cs", $$"""
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Library.Lending.Reserving;
        {{memberDefinition}}
        public record PlantId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record BookReserved(string Title);
        [EventType] public record ReservationRefused(string Title);
        public class Reservation : AggregateRoot
        {
            public Task Reserve({{parameters}})
            {
                if ({{comparison}})
                {
                    return Apply(new BookReserved("Book"));
                }
                return Apply(new ReservationRefused("Book"));
            }
        }
        """)
    ];
}
