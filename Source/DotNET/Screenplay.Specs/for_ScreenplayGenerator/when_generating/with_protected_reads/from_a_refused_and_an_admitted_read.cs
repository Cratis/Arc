// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_refused_and_an_admitted_read : a_protected_document
{
    const string History = """
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

    const string Commands = """
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }

        [Command, ReadProtection(ReadProtectionMode.On)]
        public record CleanRoom(RoomId RoomId)
        {
            public Result<RoomCleaned, ValidationResult> Handle(RoomHistory history) => new RoomCleaned();
        }
        """;

    void Because() => Generate(Library(Commands + History));

    [Fact] void should_state_the_admitted_read() => Result.Source.Contains("reads RoomOccupancy as occupancy by roomId").ShouldBeTrue();
    [Fact] void should_keep_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_withhold_the_refused_read() => Result.Source.Contains("reads RoomHistory").ShouldBeFalse();
    [Fact] void should_report_the_refusal_with_its_code() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("PLAY0781") && diagnostic.Message.Contains("history")).ShouldBeTrue();
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
}
