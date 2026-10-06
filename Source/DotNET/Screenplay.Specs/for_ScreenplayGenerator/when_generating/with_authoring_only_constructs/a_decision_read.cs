// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_decision_read : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
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
            public Result<RoomOccupancy, ValidationResult> Provide(DecisionRead<RoomOccupancy> occupancy) =>
                occupancy.Instance!.FreeBeds >= Guests ? occupancy.Instance : ValidationResult.Error("The room is full");
            public RoomBooked Handle(RoomOccupancy room) => new(Guests, room.FreeBeds);
        }
        """);

    [Fact] void should_declare_the_token_read_with_its_alias() => Result.Source.ShouldContain("reads RoomOccupancy as occupancy by roomId");
    [Fact] void should_keep_the_success_condition_of_a_conditional() => Result.Source.ShouldContain("require occupancy.freeBeds >= guests");
    [Fact] void should_map_the_forwarded_read_without_an_extra_instance_path() => Result.Source.ShouldContain("freeBeds Int = occupancy.freeBeds");
    [Fact] void should_read_the_model_once() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Reads.Count.ShouldEqual(1);
    [Fact] void should_compile_and_reject_only_known_read_admission() => AssertAuthoringDocument(legacyReads: true);
    [Fact] void should_report_provisioning_without_the_option() => Off.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning).ShouldBeTrue();
}
