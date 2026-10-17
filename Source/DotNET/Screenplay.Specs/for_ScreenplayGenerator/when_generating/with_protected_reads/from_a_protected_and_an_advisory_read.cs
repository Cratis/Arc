// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_protected_and_an_advisory_read : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy, [Unprotected] RoomOccupancy advisory) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """));

    [Fact] void should_state_the_protected_read() => Authoring.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_withhold_the_advisory_read() => Authoring.Source.Contains("as advisory").ShouldBeFalse();
    [Fact] void should_report_the_withheld_read() => Authoring.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Message.Contains("advisory reads")).ShouldBeTrue();
}
