// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """)));

    [Fact] void should_generate_the_identifier_by_default() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_identifier() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_inline_the_event() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_preserve_the_destination() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
}
