// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_with_a_non_uuid_backing_type : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(Name);
        }
        """).Replace("AuthorId(Guid Value) : EventSourceId<Guid>(Value)", "AuthorId(string Value) : EventSourceId<string>(Value)", StringComparison.Ordinal)
            .Replace("new(Guid.NewGuid())", "new(Guid.NewGuid().ToString())", StringComparison.Ordinal)));

    [Fact] void should_leave_generation_in_code() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_name_the_unsupported_backing_type() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).Message.ShouldContain("backing type 'string' is not UUID");
    [Fact] void should_preserve_the_event_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
