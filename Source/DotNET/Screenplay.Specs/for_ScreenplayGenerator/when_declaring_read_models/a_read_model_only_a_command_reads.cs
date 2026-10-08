// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model written outside every slice that nothing but a command reads, in a document carrying the
/// authoring-only constructs. The command's slice declares it, once, from what the type holds.
/// </summary>
public class a_read_model_only_a_command_reads : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Rooms.Shared
        {
            [Cratis.Arc.Queries.ModelBound.ReadModel]
            public record RoomOccupancy(int FreeBeds);
        }

        namespace Library.Rooms.Booking
        {
            using Library.Rooms.Shared;

            public record RoomId(Guid Value) : EventSourceId<Guid>(Value);

            [EventType] public record RoomBooked(int Guests, int FreeBeds);

            [Command] public record BookRoom(RoomId RoomId, int Guests)
            {
                public RoomBooked Handle(RoomOccupancy room) => new(Guests, room.FreeBeds);
            }
        }
        """);

    [Fact] void should_declare_it_in_the_commands_slice() => Result.Model.Slices.Single(_ => _.Namespace == "Library.Rooms.Booking").ReadModels.Select(_ => _.Name).ShouldContainOnly(["RoomOccupancy"]);
    [Fact] void should_declare_it_once() => Result.Source.Split('\n').Count(_ => _.Trim() == "readmodel RoomOccupancy").ShouldEqual(1);
    [Fact] void should_compile_without_findings() => Compiled.Diagnostics.Where(_ => _.Severity is Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error or Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).Select(_ => _.Message).ShouldBeEmpty();
}
