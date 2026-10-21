// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_destination_with_reassigned_events : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue] AuthorId authorId)
            {
                AuthorRegistered recorded;
                recorded = new(Name);
                AuthorRegistered alias;
                alias = recorded;
                return new(authorId, alias);
            }
        }
        """));

    [Fact] void should_withhold_the_reassigned_event_without_retargeting() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
    [Fact] void should_report_the_unavailable_destination() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
    [Fact] void should_keep_the_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
