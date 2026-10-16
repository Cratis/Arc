// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class date_provisioning_guards : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Monads;
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record RoomBooked(DateOnly Start, DateOnly End);
        [ReadModel] public record RoomOccupancy(int FreeBeds)
        {
            public static RoomOccupancy Occupancy() => new(0);
        }
        [Command] public record BookRoom(RoomId RoomId, DateOnly Start, DateOnly End)
        {
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room)
            {
                if (End < Start)
                    return ValidationResult.Error("The window is inverted");
                return room;
            }
            public RoomBooked Handle(RoomOccupancy room) => new(Start, End);
        }
        """);

    [Fact] void should_recover_the_date_requirement() => Result.Source.ShouldContain("require end >= start\n            message \"The window is inverted\"");
    [Fact] void should_round_trip_with_only_the_existing_legacy_read_limit() => AssertAuthoringDocument(legacyReads: true);
}
