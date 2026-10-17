// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_an_ambiguous_key_named_by_lookup : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, RoomId OtherRoom, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle([LookUpBy(nameof(OtherRoom))] RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """));

    [Fact] void should_read_by_the_named_property() => Result.Source.Contains("reads RoomOccupancy as occupancy by otherRoom").ShouldBeTrue();
}
