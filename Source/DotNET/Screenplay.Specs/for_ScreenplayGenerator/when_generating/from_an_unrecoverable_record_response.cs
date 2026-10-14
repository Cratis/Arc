// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_record_response : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        public record Receipt(AuthorId Copy, string Name);
        [Command] public record RegisterAuthor(string Name)
        {
            public (Receipt, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId) => (new(authorId, Name), new(Name));
        }
        """));

    [Fact] void should_withhold_the_whole_response_record() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("returns", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_event() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces", StringComparison.Ordinal) && document.Result.Source.Contains("name = name", StringComparison.Ordinal));
    [Fact] void should_report_the_unavailable_response() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.Contains("response depends", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
