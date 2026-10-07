// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class reassigned_read_parameters : an_authoring_document
{
    [Theory]
    [InlineData(false, "room = new(0);")]
    [InlineData(true, "room = new(0);")]
    [InlineData(false, "Reset(ref room);")]
    [InlineData(true, "Reset(ref room);")]
    public void should_keep_the_dependency_but_not_map_from_a_reassigned_handle_parameter(bool forwarded, string write)
    {
        GenerateCommand(forwarded ? "public RoomOccupancy Provide(RoomOccupancy room) { return room; }" : string.Empty, write);
        Result.Source.ShouldContain("reads RoomOccupancy as room by roomId");
        Result.Source.ShouldNotContain("= room.freeBeds");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Message.Contains("reassigned", StringComparison.Ordinal)).ShouldBeTrue();
        AssertAuthoringDocument(legacyReads: true);
    }

    [Fact]
    public void should_not_recover_requirements_from_a_reassigned_provide_parameter()
    {
        GenerateCommand(
            """
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room)
            {
                room = new(0);
                if (room.FreeBeds < Guests)
                    return ValidationResult.Error("The room is full");
                return room;
            }
            """,
            string.Empty);
        Result.Source.ShouldContain("reads RoomOccupancy as room by roomId");
        Result.Source.ShouldNotContain("require room.freeBeds");
        Result.Source.ShouldNotContain("= room.freeBeds");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Message.Contains("reassigned", StringComparison.Ordinal)).ShouldBeTrue();
        AssertAuthoringDocument(legacyReads: true);
    }

    void GenerateCommand(string provide, string write) => Generate($$"""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Monads;
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record RoomBooked(int Guests, int FreeBeds);
        [ReadModel] public record RoomOccupancy(int FreeBeds)
        {
            public static RoomOccupancy Occupancy() => new(0);
        }
        public class OccupancyProjection : IProjectionFor<RoomOccupancy>
        {
            public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder.From<RoomBooked>(from => from.Set(room => room.FreeBeds).To(e => e.FreeBeds));
        }
        [Command] public record BookRoom(RoomId RoomId, int Guests)
        {
            {{provide}}
            static void Reset(ref RoomOccupancy room) => room = new(0);
            public RoomBooked Handle(RoomOccupancy room)
            {
                {{write}}
                return new RoomBooked(Guests, room.FreeBeds);
            }
        }
        """);
}
