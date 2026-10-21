// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_production_condition : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [EventType] public record AuthorConfirmed(string Name);
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorRegistered, AuthorConfirmed) Handle([GeneratedValue] AuthorId authorId)
            {
                var alias = authorId;
                return (alias.Value == Guid.Empty ? new AuthorRegistered(Name) : new AuthorRegistered("Other"), new AuthorConfirmed(Name));
            }
        }
        """));

    [Fact] void should_not_weaken_the_condition_to_unconditional_production() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_production() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces AuthorConfirmed", StringComparison.Ordinal));
    [Fact] void should_report_the_dependent_production() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("condition depends", StringComparison.Ordinal)) == 2);
    [Fact] void should_not_add_a_handler_beside_the_survivor() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("handler", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
