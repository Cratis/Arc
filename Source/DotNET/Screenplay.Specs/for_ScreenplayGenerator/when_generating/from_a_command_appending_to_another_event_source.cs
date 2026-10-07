// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_appending_to_another_event_source : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        [EventType]
        public record AuthorRegistered(string Name);
        [Command]
        public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
        {
            public async Task Handle(IEventLog eventLog) => await eventLog.Append(OtherId, new AuthorRegistered(Name));
        }
        """));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_not_retarget_the_append() => Result.Source.ShouldNotContain("for id");
    [Fact] void should_not_mark_the_context_identifier() => Result.Source.ShouldNotContain(" identifier");
    [Fact] void should_report_the_unrepresented_destination() => Result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
