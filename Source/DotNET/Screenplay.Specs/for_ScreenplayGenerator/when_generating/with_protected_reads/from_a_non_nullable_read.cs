// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_non_nullable_read : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy)
            {
                if (occupancy.FreeBeds < Guests) return ValidationResult.Error("The room is full");
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """));

    [Fact] void should_state_the_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_state_the_requirement() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
