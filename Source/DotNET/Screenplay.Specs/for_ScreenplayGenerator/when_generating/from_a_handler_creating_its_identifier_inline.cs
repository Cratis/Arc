// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_handler_creating_its_identifier_inline : a_generated_document
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

    [Fact] void should_preserve_the_legacy_generation_heuristic() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_preserve_the_legacy_response() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_not_claim_an_explicit_pinning_contract() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.GeneratedParameters.ShouldBeFalse();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
