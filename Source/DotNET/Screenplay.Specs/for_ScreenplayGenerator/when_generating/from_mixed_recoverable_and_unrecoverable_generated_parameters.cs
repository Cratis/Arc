// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_mixed_recoverable_and_unrecoverable_generated_parameters : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        public record ReceiptId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<ReceiptId>
        {
            public static ReceiptId New() => new(Guid.NewGuid());
        }
        [EventType] public record ReceiptIssued(ReceiptId Receipt);
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorRegistered, ReceiptIssued) Handle([GeneratedValue] AuthorId authorId, [GeneratedValue("receipt")] ReceiptId receipt)
                => (new(authorId, Name), new(receipt));
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(AuthorId Copy, string Name);", StringComparison.Ordinal));

    [Fact] void should_keep_the_independent_generated_parameter_and_production() => Documents.Where(document => document.Options.MaximumExecutableModelVersion != SemanticVersion.V6).ShouldEachConformTo(document => document.Result.Source.Contains("receipt ReceiptId generated", StringComparison.Ordinal) && document.Result.Source.Contains("produces", StringComparison.Ordinal) && document.Result.Source.Contains("ReceiptIssued", StringComparison.Ordinal) && document.Result.Source.Contains("receipt = receipt", StringComparison.Ordinal));
    [Fact] void should_withhold_only_the_dependent_production() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal));
    [Fact] void should_report_the_required_payload_loss() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("required event property 'Copy'", StringComparison.Ordinal)));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
