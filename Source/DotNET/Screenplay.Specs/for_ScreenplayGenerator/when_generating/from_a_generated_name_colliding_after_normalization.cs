// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_name_colliding_after_normalization : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Id, string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId _id = new(Guid.NewGuid());
                return (_id, new(Name));
            }
        }
        """)));

    [Fact] void should_preserve_the_input_property() => Result.Source.ShouldContain("id String");
    [Fact] void should_not_emit_generation() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_not_emit_the_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_not_infer_the_generated_destination() => Result.Source.ShouldNotContain("for id");
    [Fact] void should_report_the_collision() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("collides", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_bind_and_round_trip_the_legacy_document() => AssertDocument();
}
