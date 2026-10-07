// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_provisioning_guard : an_authoring_document
{
    void Because() => Generate("""
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
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room)
            {
                if (room.FreeBeds < Guests)
                    return ValidationResult.Error("The room is full");
                return room;
            }
            public RoomBooked Handle(RoomOccupancy room) => new(Guests, room.FreeBeds);
        }
        """);

    [Fact] void should_read_by_the_command_key() => Result.Source.ShouldContain("reads RoomOccupancy as room by roomId");
    [Fact] void should_invert_the_rejection_condition() => Result.Source.ShouldContain("require room.freeBeds >= guests");
    [Fact] void should_preserve_the_error_message() => Result.Source.ShouldContain("message \"The room is full\"");
    [Fact] void should_map_from_the_read_instance() => Result.Source.ShouldContain("freeBeds Int = room.freeBeds");
    [Fact] void should_not_invent_a_provide_block() => Result.Source.Contains("provide", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_compile_and_report_only_unsupported_binding() => AssertAuthoringDocument(legacyReads: true);
    [Fact] void should_omit_reads_when_disabled() => Off.Source.Contains("reads RoomOccupancy", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_opt_in_when_disabled() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning).Message.ShouldContain("AuthoringOnlyConstructs");
}
