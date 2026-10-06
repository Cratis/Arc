// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_with_a_validator : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId>
        {
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """)));

    [Fact] void should_not_generate_a_validated_concept() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_not_return_an_unavailable_value() => Result.Source.ShouldNotContain("returns authorId");
    [Fact] void should_not_inline_or_retarget_the_event() => Result.Source.ShouldNotContain("for authorId");
    [Fact] void should_report_the_response_omission() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("concept validators");
    [Fact] void should_report_the_identity_omission() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeTrue();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_not_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
}
