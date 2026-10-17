// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_an_assembly_protected_by_default : a_protected_document
{
    const string Assembly = "[assembly: Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)]";

    const string Command = """
        [Command]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """;

    void Because() => Generate(Library(Command, attributes: Assembly));

    [Fact] void should_state_the_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
