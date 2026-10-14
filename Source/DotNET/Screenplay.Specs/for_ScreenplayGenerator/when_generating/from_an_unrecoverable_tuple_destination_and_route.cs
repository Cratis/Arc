// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_tuple_destination_and_route : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command, EventSourceType("Authors"), EventStreamType("Registration"), EventStreamId("{StreamId}")]
        public record RegisterAuthor(string Name, string StreamId)
        {
            public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId)
            {
                var destination = authorId;
                return (destination, new(Name));
            }
        }
        """));

    [Fact] void should_withhold_the_production_and_response() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal) && !document.Result.Source.Contains("returns", StringComparison.Ordinal));
    [Fact] void should_remove_the_dependent_route_including_its_stream_id() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("stream Authors Registration", StringComparison.Ordinal) && !document.Result.Source.Contains("streamId = streamId", StringComparison.Ordinal));
    [Fact] void should_explain_the_destination_and_whole_route_loss() => Documents.Where(document => document.Options.AuthoringOnlyConstructs || document.Options.MaximumExecutableModelVersion is null).ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)) && document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute));
    [Fact] void should_keep_the_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
