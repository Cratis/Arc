// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_provide_guard : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomOccupancy, ValidationResult> Provide(RoomOccupancy occupancy) =>
                occupancy.FreeBeds < Guests ? ValidationResult.Error("The room is full") : occupancy;

            public RoomBooked Handle(RoomOccupancy occupancy) => new(Guests, occupancy.FreeBeds);
        }
        """));

    [Fact] void should_state_the_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_state_the_requirement() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
