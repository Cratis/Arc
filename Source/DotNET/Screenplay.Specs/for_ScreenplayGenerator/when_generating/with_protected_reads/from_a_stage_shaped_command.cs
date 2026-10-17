// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_stage_shaped_command : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle([LookUpBy(nameof(RoomId))] RoomOccupancy? occupancy)
            {
                if (occupancy is null) return ValidationResult.Error("Room missing");
                if (occupancy.FreeBeds < Guests) return ValidationResult.Error("The room is full");
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """));

    [Fact] void should_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_state_the_read_by_its_key() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_state_the_requirement() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_state_the_read_path_mapping() => Result.Source.Contains("freeBeds = occupancy.freeBeds").ShouldBeTrue();
    [Fact] void should_report_the_absence_message_difference() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("Room missing")).ShouldEqual(1);
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
    [Fact] void should_state_the_same_document_in_authoring_mode() => Authoring.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
