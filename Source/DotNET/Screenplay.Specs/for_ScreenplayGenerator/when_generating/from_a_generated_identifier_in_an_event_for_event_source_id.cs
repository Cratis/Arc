// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier_in_an_event_for_event_source_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue("author")] AuthorId authorId) => new(authorId, new AuthorRegistered(Name));
        }
        """)));

    [Fact] void should_recover_the_explicit_destination() => Result.Source.ShouldContain("author AuthorId generated identifier");
    [Fact] void should_route_the_inline_event_to_the_parameter() => Result.Source.ShouldContain("for author");
    [Fact] void should_inline_the_event() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_not_invent_a_response_from_the_wrapper() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
