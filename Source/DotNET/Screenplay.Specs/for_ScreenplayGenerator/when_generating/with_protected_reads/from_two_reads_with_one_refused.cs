// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_two_reads_with_one_refused : a_protected_document
{
    const string Cleanings = """
        [EventType] public record RoomCleaned();
        [ReadModel]
        public record RoomHistory(RoomId Id, int Cleanings)
        {
            public static RoomHistory? ById(RoomId id) => null;
        }
        public class HistoryProjection : IProjectionFor<RoomHistory>
        {
            public void Define(IProjectionBuilderFor<RoomHistory> builder) => builder.From<RoomCleaned>(from => from.Count(room => room.Cleanings));
        }
        """;

    const string Command = """
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy, RoomHistory history)
            {
                if (occupancy.FreeBeds < Guests) return ValidationResult.Error("The room is full");
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """;

    void Because() => Generate(Library(Command + Cleanings));

    [Fact] void should_state_the_admitted_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_withhold_only_the_refused_read() => Result.Source.Contains("reads RoomHistory").ShouldBeFalse();
    [Fact] void should_keep_the_requirement_over_the_admitted_read() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_keep_the_mapping_from_the_admitted_read() => Result.Source.Contains("freeBeds = occupancy.freeBeds").ShouldBeTrue();
    [Fact] void should_report_one_refusal_for_the_refused_read() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("'history'")).ShouldEqual(1);
    [Fact] void should_not_report_a_refusal_for_the_admitted_read() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("'occupancy'") && diagnostic.Message.Contains("refused")).ShouldBeFalse();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
