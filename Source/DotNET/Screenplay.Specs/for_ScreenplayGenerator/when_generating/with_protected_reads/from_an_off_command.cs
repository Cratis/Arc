// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_an_off_command : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.Off)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """));

    [Fact] void should_not_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeFalse();
    [Fact] void should_not_state_the_read() => Result.Source.Contains("reads RoomOccupancy").ShouldBeFalse();
    [Fact] void should_state_the_legacy_read_in_authoring_mode() => Authoring.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
}
