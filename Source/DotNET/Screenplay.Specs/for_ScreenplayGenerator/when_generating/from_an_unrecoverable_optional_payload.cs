// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_optional_payload : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(authorId.Value.ToString(), Name);
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(string? Copy, string Name);", StringComparison.Ordinal));

    [Fact] void should_keep_the_production_and_independent_mapping() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal) && document.Result.Source.Contains("name = name", StringComparison.Ordinal));
    [Fact] void should_omit_only_the_dependent_optional_mapping() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("copy =", StringComparison.Ordinal));
    [Fact] void should_classify_the_optional_loss_as_warning() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Severity == ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_emit_an_illegal_handler_beside_produces() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
