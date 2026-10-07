// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_optional_generated_local : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId?, AuthorRegistered) Handle()
            {
                AuthorId? authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """)));

    [Fact] void should_not_turn_an_optional_local_into_required_generation() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_not_return_an_unavailable_value() => Result.Source.ShouldNotContain("returns authorId");
    [Fact] void should_explain_the_unadmitted_shape() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("required scalar");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_not_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
}
