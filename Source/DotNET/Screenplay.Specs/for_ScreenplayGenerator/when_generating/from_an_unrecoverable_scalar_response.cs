// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_scalar_response : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorId Handle([GeneratedValue] AuthorId authorId) => authorId;
        }
        """));

    [Fact] void should_withhold_the_response() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("returns", StringComparison.Ordinal));
    [Fact] void should_keep_the_opaque_response_handler() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_report_the_unavailable_response_as_information() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.Contains("response depends", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
