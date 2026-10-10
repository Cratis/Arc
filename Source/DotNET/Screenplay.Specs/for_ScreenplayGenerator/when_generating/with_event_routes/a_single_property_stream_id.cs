// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_single_property_stream_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command, EventSourceType("Account"), EventStreamType("Transactions"), EventStreamId("{Name}")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_declare_the_property_type_as_the_stream_key() => Result.Source.ShouldContain("streamId String");
    [Fact] void should_state_the_route() => Result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_map_the_direct_property() => Result.Source.ShouldContain("streamId = name");
    [Fact] void should_not_emit_the_placeholder_as_a_literal() => Result.Source.ShouldNotContain("{Name}");
    [Fact] void should_not_report_an_unreadable_route() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldBeEmpty();
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
