// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_refused_read_in_a_multi_line_command : a_protected_document
{
    const string Commands = """
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

        [Command, ReadProtection(ReadProtectionMode.On)]
        public record AuditRoom(RoomId RoomId)
        {
            public Result<RoomCleaned, ValidationResult> Handle(RoomHistory history)
            {
                if (history.Cleanings > 5) return ValidationResult.Error("The room is audited too often");
                return new RoomCleaned();
            }
        }

        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy)
            {
                if (occupancy.FreeBeds < Guests) return ValidationResult.Error("The room is full");
                return new RoomBooked(Guests, occupancy.FreeBeds);
            }
        }
        """;

    void Because() => Generate(Library(Commands));

    [Fact] void should_withhold_the_read_of_the_earlier_command() => Result.Source.Contains("reads RoomHistory").ShouldBeFalse();
    [Fact] void should_withhold_the_requirement_over_the_refused_read() => Result.Source.Contains("history.cleanings").ShouldBeFalse();
    [Fact] void should_keep_the_read_of_the_later_command() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_keep_the_requirement_of_the_later_command() => Result.Source.Contains("require occupancy.freeBeds >= guests").ShouldBeTrue();
    [Fact] void should_report_no_defect() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
