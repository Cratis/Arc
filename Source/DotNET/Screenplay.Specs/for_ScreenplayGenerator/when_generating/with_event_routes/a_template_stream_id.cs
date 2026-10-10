// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_template_stream_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command, EventSourceType("Account"), EventStreamType("Transactions"), EventStreamId("{AuthorId}:{Month}")]
        public record RegisterAuthor(AuthorId AuthorId, string Month, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_report_the_unreadable_template() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("property-derived");
    [Fact] void should_withhold_the_entire_route() => Result.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_not_misstate_the_template_as_a_literal() => Result.Source.ShouldNotContain("{AuthorId}:{Month}");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
