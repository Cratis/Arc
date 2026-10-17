// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_success_factory_return : a_protected_document
{
    void Because() => Generate(Library("""
        public static class Result
        {
            public static ValidationResult Success(ValidationResult value) => value;
        }
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy)
            {
                if (occupancy.FreeBeds < Guests) return Result.Success(ValidationResult.Error("The room is full"));
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """));

    [Fact] void should_not_read_the_factory_as_a_rejection() => Result.Source.Contains("require occupancy.freeBeds").ShouldBeFalse();
}
