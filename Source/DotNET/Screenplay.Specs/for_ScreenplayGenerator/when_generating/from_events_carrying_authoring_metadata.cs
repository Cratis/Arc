// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Event descriptions, remarks, and persisted names survive generation without introducing redundant pins.
/// </summary>
public class from_events_carrying_authoring_metadata : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        /// <summary>An author acquired a name.</summary>
        /// <remarks>
        /// The **name** is the author's display name.
        ///
        /// It is not the event source identity.
        /// </remarks>
        [EventType("AuthorNamed")]
        public record AuthorRegistered(string Name);

        [EventType("AuthorRemoved")]
        public record AuthorRemoved(string Name);

        [EventType]
        public record AuthorNotified(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_emit_the_summary() => Result.Source.ShouldContain("description \"An author acquired a name.\"");
    [Fact] void should_emit_a_markdown_documentation_fence() => Result.Source.ShouldContain("```markdown");
    [Fact] void should_keep_the_remarks() => Result.Source.ShouldContain("The **name** is the author's display name.");
    [Fact] void should_keep_the_second_paragraph() => Result.Source.ShouldContain("It is not the event source identity.");
    [Fact] void should_pin_the_previous_name() => Result.Source.ShouldContain("id \"AuthorNamed\"");
    [Fact] void should_not_pin_the_current_name() => Result.Source.ShouldNotContain("id \"AuthorRemoved\"");
    [Fact] void should_not_invent_a_pin() => Result.Source.ShouldNotContain("id \"AuthorNotified\"");
    [Fact] void should_not_report_a_redundant_pin() => RoundTrip.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0471").ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
