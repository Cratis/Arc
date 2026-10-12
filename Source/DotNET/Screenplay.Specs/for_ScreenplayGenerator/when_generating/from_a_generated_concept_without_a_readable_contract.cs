// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_without_a_readable_contract : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(Name);
        }
        """).Replace(", IGeneratable<AuthorId>", string.Empty, StringComparison.Ordinal)));

    [Fact] void should_not_infer_generation_from_a_factory_name() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_preserve_the_event_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_explain_the_missing_contract() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).Message.ShouldContain("New() has no readable IGeneratable<T> implementation");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
