// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier_with_a_standalone_event : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Name, bool Register)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        public static class OtherProducer
        {
            public static AuthorRegistered Create() => new("Other");
        }
        """)));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event");
    [Fact] void should_emit_plain_produces() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_supply_the_explicit_destination() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
}
