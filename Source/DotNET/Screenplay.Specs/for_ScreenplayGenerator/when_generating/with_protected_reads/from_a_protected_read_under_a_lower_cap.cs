// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_protected_read_under_a_lower_cap : a_protected_document
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

    [Fact] void should_keep_todays_output() => Capped.Source.Contains("command reads protected").ShouldBeFalse();
    [Fact] void should_not_state_the_read() => Capped.Source.Contains("reads RoomOccupancy").ShouldBeFalse();
    [Fact] void should_warn_that_v25_is_required() => Capped.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning && diagnostic.Message.Contains("ESM v25")).ShouldEqual(1);
}
