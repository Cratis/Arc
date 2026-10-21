// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_destination_with_a_held_event : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue] AuthorId authorId)
            {
                var destination = authorId;
                var recorded = new AuthorRegistered(Name);
                var alias = recorded;
                return new(destination, alias);
            }
        }
        """));

    [Fact] void should_withhold_the_held_event_without_retargeting() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
    [Fact] void should_report_the_destination_loss() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
    [Fact] void should_keep_the_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
