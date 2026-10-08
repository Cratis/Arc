// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class a_generated_identifier : a_generated_document
{
    const string Source = """
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """;

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V6 }, (Analyzed.SlicePath, IdentifierSources.With(Source)));

    [Fact] void should_withhold_the_generated_identifier() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_withhold_the_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_keep_the_legacy_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_not_inline_the_event() => Result.Source.ShouldNotContain("produces event");
    [Fact] void should_not_invent_a_destination() => Result.Source.ShouldNotContain("for authorId");
    [Fact] void should_remove_the_unused_generated_concept() => Result.Source.ShouldNotContain("concept AuthorId");
    [Fact] void should_report_the_cap_with_sp0052() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("MaximumExecutableModelVersion is capped at ESM v6.0");
    [Fact] void should_not_report_the_capped_identity_as_unadmitted() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_bind_below_v7() => SemanticVersion.V6.IsAtLeast(Bound.Value!.Model.SemanticVersion).ShouldBeTrue();
}
