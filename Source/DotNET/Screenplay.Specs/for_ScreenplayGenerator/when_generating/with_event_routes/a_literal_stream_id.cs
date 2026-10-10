// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_literal_stream_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command, EventSourceType("Account"), EventStreamType("Transactions"), EventStreamId("October")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_declare_a_scalar_key() => Result.Source.ShouldContain("streamId String");
    [Fact] void should_map_the_literal() => Result.Source.ShouldContain("streamId = \"October\"");
    [Fact] void should_not_report_an_unreadable_route() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldBeFalse();
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
