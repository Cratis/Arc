// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_stream_scoped_read : a_protected_document
{
    const string View = """
        [EventType] public record RoomOpened(int Beds);
        [EventType] public record RoomClosed();
        [ReadModel, EventStream<RoomStream>]
        public record RoomOccupancy(RoomId Id, int Beds)
        {
            public static RoomOccupancy? ById(RoomId id) => null;
        }
        public class RoomStream;
        public class OccupancyProjection : IProjectionFor<RoomOccupancy>
        {
            public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder
                .From<RoomOpened>(from => from.Set(room => room.Beds).To(e => e.Beds))
                .RemovedWith<RoomClosed>();
        }
        """;

    const string Command = """
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record OpenRoom(RoomId RoomId)
        {
            public Result<RoomOpened, ValidationResult> Handle(RoomOccupancy occupancy) => new RoomOpened(occupancy.Beds);
        }
        """;

    void Because() => Generate(Library(Command, View, "using Cratis.Chronicle.ReadModels;"));

    [Fact] void should_not_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeFalse();
    [Fact] void should_not_state_an_unguarded_read() => Result.Source.Contains("reads RoomOccupancy").ShouldBeFalse();
    [Fact] void should_refuse_with_the_reason_naming_the_stream_scope_and_its_issue() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("stream-scoped") && diagnostic.Message.Contains("Screenplay#805")).ShouldBeTrue();
    [Fact] void should_not_fail_to_bind() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
