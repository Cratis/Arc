// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_with_a_validator : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId>
        {
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId)
            {
                return (authorId, new(Name));
            }
        }
        """)));

    [Fact] void should_admit_an_empty_validator() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_generated_identity() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_retarget_the_event() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_not_report_withheld_generation() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_keep_the_rule_free_version() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
}
