// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_tuple_event_handler : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        [EventType]
        public record AuthorRegistered(string Name);
        [Command]
        public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
        {
            public (Guid, AuthorRegistered) Handle() => (OtherId, new(Name));
        }
        """));

    [Fact] void should_inline_the_returned_event() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_use_the_command_context_not_the_raw_guid_response() => Result.Source.ShouldContain("for id");
    [Fact] void should_mark_the_context_identifier() => Result.Source.ShouldContain(" identifier");
    [Fact] void should_not_report_an_unproven_tuple_identity() => Result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldEqual(0);
    [Fact] void should_not_report_an_unrepresented_destination() => Result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(0);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
