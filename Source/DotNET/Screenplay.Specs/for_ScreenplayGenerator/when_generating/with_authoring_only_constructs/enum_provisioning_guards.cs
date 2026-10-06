// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class enum_provisioning_guards : an_authoring_document
{
    [Theory]
    [InlineData("room.Status == RoomStatus.Closed ? ValidationResult.Error(\"Closed\") : room", "!=")]
    [InlineData("room.Status != RoomStatus.Closed ? ValidationResult.Error(\"Not closed\") : room", "==")]
    [InlineData("room.Status == RoomStatus.Closed ? room : ValidationResult.Error(\"Not closed\")", "==")]
    void should_name_the_enum_member_and_preserve_acceptance(string body, string comparison)
    {
        Generate(Source(body));
        Result.Source.ShouldContain($"require room.status {comparison} \"closed\"");
        var requirement = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Requirements.Single();
        ((ComparisonCondition)requirement.Condition).Right.ShouldEqual(new LiteralSource(new EnumValue("Closed")));
        AssertAuthoringDocument(legacyReads: true);
    }

    [Fact]
    void should_omit_an_unnamed_enum_operand()
    {
        Generate(Source("room.Status == (RoomStatus)42 ? ValidationResult.Error(\"Unknown\") : room"));
        Result.Source.Contains("require ", StringComparison.Ordinal).ShouldBeFalse();
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Location == "Library.Rooms.Booking.BookRoom")
            .Message.ShouldContain("provisioning behavior");
    }

    static string Source(string body) => """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Monads;
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        public enum RoomStatus { Open, Closed }
        [EventType] public record RoomBooked(RoomStatus Status);
        [ReadModel] public record RoomOccupancy(RoomStatus Status);
        public class OccupancyProjection : IProjectionFor<RoomOccupancy>
        {
            public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder.From<RoomBooked>(from => from.Set(room => room.Status).To(e => e.Status));
        }
        [Command] public record BookRoom(RoomId RoomId)
        {
            public RoomBooked Handle(RoomOccupancy room) => new(room.Status);
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room) =>
        """ + body + "; }";
}
