// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_an_optional_key : a_protected_document
{
    const string Command = """
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, RoomId? OtherRoom, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle([LookUpBy(nameof(OtherRoom))] RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """;

    void Because() => Generate(Library(Command));

    [Fact] void should_not_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeFalse();
    [Fact] void should_not_state_an_unguarded_read() => Result.Source.Contains("reads RoomOccupancy").ShouldBeFalse();
    [Fact] void should_report_the_refusal_with_its_code() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("PLAY0283")).ShouldBeTrue();
    [Fact] void should_still_compile() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
