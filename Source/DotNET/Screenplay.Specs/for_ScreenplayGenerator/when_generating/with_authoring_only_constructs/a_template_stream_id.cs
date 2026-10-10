// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_template_stream_id : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Command]
        [EventSourceType("Account")]
        [EventStreamType("Transactions")]
        [EventStreamId("{AuthorId}:{Month}")]
        public record RegisterAuthor(AuthorId AuthorId, string Month, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_report_the_property_derived_mapping_left_in_code() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("property-derived");
    [Fact] void should_not_emit_the_template_as_a_literal_stream_id() => Result.Source.ShouldNotContain("{AuthorId}:{Month}");
}
