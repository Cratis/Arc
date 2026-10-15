// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_required_mapping_of_an_unrecoverable_generated_value : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(authorId, Name);
        }
        """).Replace("public static AuthorId New() => new(Guid.NewGuid());", "public static AuthorId New() => new(Guid.Empty);", StringComparison.Ordinal)
            .Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(AuthorId Copy, string Name);", StringComparison.Ordinal)));

    [Fact] void should_report_the_unrecoverable_generation() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldEqual(1);
    [Fact] void should_withhold_the_incomplete_production() => Result.Source.ShouldNotContain("produces AuthorRegistered");
    [Fact] void should_keep_the_handler_pointer() => Result.Source.ShouldContain("handler");
    [Fact] void should_report_the_required_mapping_loss() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("required event property 'Copy'");
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
    [Fact] void should_not_fail_binding_verification() => Assert.True(Result.Diagnostics.All(diagnostic => diagnostic.Code != ScreenplayDiagnosticCodes.DocumentDidNotBind), string.Join(Environment.NewLine, Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
}
