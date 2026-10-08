// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
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
    [Fact] void should_keep_identical_output_with_a_v7_cap() => new ScreenplayEmitter().Emit(Result.Model, new() { MaximumExecutableModelVersion = SemanticVersion.V7 }).Source.ShouldEqual(Result.Source);
    [Fact] void should_withhold_v7_constructs_with_a_cap_in_authoring_mode()
    {
        var capped = new ScreenplayEmitter().Emit(Result.Model, new() { MaximumExecutableModelVersion = SemanticVersion.V6, AuthoringOnlyConstructs = true });
        capped.Source.ShouldNotContain("generated");
        capped.Source.ShouldNotContain("returns");
        capped.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("ESM v6.0", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
