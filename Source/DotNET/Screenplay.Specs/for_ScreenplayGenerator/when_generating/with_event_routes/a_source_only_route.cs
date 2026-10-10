// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_source_only_route : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Command, EventSourceType("Account")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_report_the_missing_stream_once_by_default() => Off.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(1);
    [Fact] void should_report_the_missing_stream_once_in_authoring_mode() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(1);
    [Fact] void should_name_the_missing_stream() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("without a selected stream");
    [Fact] void should_withhold_the_incomplete_default_route() => Off.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_keep_the_readable_authoring_declaration() => Result.Source.ShouldContain("eventsource Account");
    [Fact] void should_not_report_a_default_binding_failure() => Off.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
}
