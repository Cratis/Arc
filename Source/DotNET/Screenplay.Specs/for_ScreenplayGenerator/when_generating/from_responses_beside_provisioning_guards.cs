// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_responses_beside_provisioning_guards : a_generated_document
{
    [Theory]
    [InlineData("public int Handle(RoomOccupancy room) => Guests;")]
    [InlineData("public void Handle(RoomOccupancy room) { }")]
    [InlineData("public RoomId Handle(RoomOccupancy room) => new RoomId(Guid.NewGuid());")]
    public void should_keep_the_handler_and_withhold_response_and_generation(string handler)
    {
        var source = """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Arc.Validation;
            using Cratis.Chronicle.Events;
            using Cratis.Monads;
            namespace Library.Rooms.Booking;
            public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
            [ReadModel] public record RoomOccupancy(int FreeBeds);
            [Command] public record BookRoom(RoomId RoomId, int Guests)
            {
                public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy room)
                {
                    if (room.FreeBeds < Guests)
                        return ValidationResult.Error("The room is full");
                    return room;
                }
            """ + handler + "}";
        Analyzed.ErrorsIn(Analyzed.Compile((Analyzed.SlicePath, source))).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));

        Result.Source.ShouldContain("handler");
        Result.Source.ShouldContain("file");
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("generated");
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().HasNoFactBehavior.ShouldBeFalse();
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse);
        diagnostic.Location.ShouldEqual("Library.Rooms.Booking.BookRoom");
        diagnostic.Message.ShouldContain("command 'BookRoom'");
        new ScreenplayCompiler().Compile(Result.Source).Success.ShouldBeTrue();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
