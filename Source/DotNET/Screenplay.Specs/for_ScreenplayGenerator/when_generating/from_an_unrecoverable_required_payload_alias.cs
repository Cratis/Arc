// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_required_payload_alias : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue("receipt")] AuthorId authorId)
            {
                var alias = authorId;
                var copy = alias;
                return new(copy.Value, Name);
            }
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(Guid Copy, string Name);", StringComparison.Ordinal));

    [Fact] void should_withhold_the_dependent_production_in_every_mode() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal));
    [Fact] void should_keep_the_handler_and_event_schema() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal) && document.Result.Source.Contains("event AuthorRegistered", StringComparison.Ordinal));
    [Fact] void should_classify_the_required_loss_as_information() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Severity == ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_name_the_required_member_and_generated_origin() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.Contains("required event property 'Copy'", StringComparison.Ordinal) && document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.Contains("generated input 'receipt'", StringComparison.Ordinal));
    [Fact] void should_never_invent_caller_input() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("receipt AuthorId", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
