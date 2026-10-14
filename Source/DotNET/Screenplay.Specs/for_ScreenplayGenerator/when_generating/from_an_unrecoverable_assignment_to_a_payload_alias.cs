// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_assignment_to_a_payload_alias : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId)
            {
                var copy = Guid.Empty;
                copy = authorId.Value;
                return new(copy, Name);
            }
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(Guid Copy, string Name);", StringComparison.Ordinal));

    [Fact] void should_withhold_the_required_production() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
    [Fact] void should_name_the_unavailable_required_member() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.Contains("required event property 'Copy'", StringComparison.Ordinal));
    [Fact] void should_report_information_rather_than_an_optional_mapping_warning() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Severity == ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
