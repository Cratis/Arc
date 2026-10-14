// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_wrapper_destination : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue] AuthorId authorId) => new(authorId, new AuthorRegistered(Name));
        }
        """));

    [Fact] void should_withhold_the_event_rather_than_redirect_it() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal) && !document.Result.Source.Contains("for authorId", StringComparison.Ordinal));
    [Fact] void should_keep_the_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_report_the_unavailable_destination() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
