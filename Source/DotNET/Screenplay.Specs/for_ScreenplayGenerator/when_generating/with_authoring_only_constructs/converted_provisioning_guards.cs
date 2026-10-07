// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class converted_provisioning_guards : an_authoring_document
{
    [Theory]
    [InlineData("(int)room.FreeBeds == 0")]
    [InlineData("room.FreeBeds == 0")]
    public void should_leave_converted_comparisons_in_code(string condition)
    {
        Generate(Source(condition));
        Result.Source.ShouldNotContain("require ");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Location == "Library.Rooms.Booking.BookRoom")
            .Message.ShouldContain("provisioning behavior");
        AssertAuthoringDocument(legacyReads: true);
    }

    [Fact]
    public void should_retain_an_unconverted_decimal_comparison()
    {
        Generate(Source("room.FreeBeds == 0m"));
        Result.Source.ShouldContain("require room.freeBeds != 0");
        AssertAuthoringDocument(legacyReads: true);
    }

    static string Source(string condition) => $$"""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Monads;
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record RoomBooked(decimal FreeBeds);
        [ReadModel] public record RoomOccupancy(decimal FreeBeds);
        public class OccupancyProjection : IProjectionFor<RoomOccupancy>
        {
            public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder.From<RoomBooked>(from => from.Set(room => room.FreeBeds).To(e => e.FreeBeds));
        }
        [Command] public record BookRoom(RoomId RoomId)
        {
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room)
            {
                if ({{condition}}) return ValidationResult.Error("The room is full");
                return room;
            }
            public RoomBooked Handle(RoomOccupancy room) => new(room.FreeBeds);
        }
        """;
}
