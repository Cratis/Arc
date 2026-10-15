// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_independent_wrapper_with_unrecoverable_response : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(AuthorId ExistingId, string Name)
        {
            public (AuthorId, Cratis.Chronicle.EventSequences.EventForEventSourceId) Handle([GeneratedValue] AuthorId authorId)
                => (authorId, new(ExistingId, new AuthorRegistered(Name)));
        }
        """));

    [Fact] void should_keep_the_independently_routed_event() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal) && document.Result.Source.Contains("name = name", StringComparison.Ordinal));
    [Fact] void should_not_claim_the_response_is_a_tuple_destination() => Documents.ShouldEachConformTo(document => !document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
    [Fact] void should_withhold_the_unavailable_response_without_retargeting() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("returns", StringComparison.Ordinal) && !document.Result.Source.Contains("for authorId", StringComparison.Ordinal) && !document.Result.Source.Contains("for existingId", StringComparison.Ordinal));
    [Fact] void should_report_the_unrepresented_response() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse) == 1);
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
