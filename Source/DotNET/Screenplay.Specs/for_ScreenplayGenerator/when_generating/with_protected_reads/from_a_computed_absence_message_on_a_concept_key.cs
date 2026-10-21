// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_computed_absence_message_on_a_concept_key : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle([LookUpBy(nameof(RoomId))] RoomOccupancy? occupancy)
            {
                if (occupancy is null) return ValidationResult.Error($"Room '{RoomId.Value}' does not exist.");
                if (occupancy.FreeBeds < Guests) return ValidationResult.Error("The room is full");
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """));

    [Fact] void should_state_the_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_state_the_requirement() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_report_that_the_absence_message_is_not_carried() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("not carried")).ShouldEqual(1);
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
