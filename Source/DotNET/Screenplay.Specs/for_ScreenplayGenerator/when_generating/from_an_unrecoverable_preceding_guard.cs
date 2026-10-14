// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_preceding_guard : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue] AuthorId authorId)
            {
                var alias = authorId;
                if (alias.Value == Guid.Empty) throw new InvalidOperationException();
                return new(Name);
            }
        }
        """));

    [Fact] void should_withhold_the_guarded_production() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
    [Fact] void should_report_the_guard_loss() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.Contains("condition depends", StringComparison.Ordinal));
    [Fact] void should_keep_the_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
