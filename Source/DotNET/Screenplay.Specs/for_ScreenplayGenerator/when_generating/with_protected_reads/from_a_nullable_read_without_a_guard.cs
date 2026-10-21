// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_nullable_read_without_a_guard : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy? occupancy) => new RoomBooked(Guests, occupancy?.FreeBeds ?? 0);
        }
        """));

    [Fact] void should_not_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeFalse();
    [Fact] void should_not_state_an_unguarded_read() => Result.Source.Contains("reads RoomOccupancy").ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("absence")).ShouldBeTrue();
}
