// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class UuidComparisonSources
{
    internal static string Guard(string comparison, string extraMembers = "") => $$"""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Library.Lending.Reserving;
        public record MemberId(Guid Value) : ConceptAs<Guid>(Value);
        public record PlantId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record BookReserved(string Title);
        [EventType] public record ReservationRefused(string Title);
        [Command] public record ReserveBook([Cratis.Chronicle.Keys.Key] string Id, string Title, Guid Owner, Guid Other, MemberId Member, MemberId OtherMember, PlantId Plant, Guid? Maybe, string Text, int Count)
        {
            {{extraMembers}}
            public object Handle() => {{comparison}} ? new BookReserved(Title) : new ReservationRefused(Title);
        }
        """;

    internal static string Provisioning(string comparison) => $$"""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Concepts;
        using Cratis.Monads;
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        public record MemberId(Guid Value) : ConceptAs<Guid>(Value);
        [EventType] public record RoomBooked(MemberId Member, Guid Owner);
        [ReadModel] public record RoomGrant(MemberId MemberId, Guid Owner, Guid? Optional)
        {
            public static RoomGrant Grant() => new(new(Guid.Empty), Guid.Empty, null);
        }
        public class GrantProjection : IProjectionFor<RoomGrant>
        {
            public void Define(IProjectionBuilderFor<RoomGrant> builder) => builder.From<RoomBooked>(from => from.Set(grant => grant.MemberId).To(e => e.Member).Set(grant => grant.Owner).To(e => e.Owner));
        }
        [Command] public record BookRoom(RoomId RoomId, MemberId RequestedBy, Guid Requester)
        {
            public Result<RoomGrant, ValidationResult> Provide(DecisionRead<RoomGrant> grant) =>
                {{comparison}} ? grant.Instance : ValidationResult.Error("Not allowed");
            public RoomBooked Handle(RoomGrant grant) => new(RequestedBy, Requester);
        }
        """;
}
