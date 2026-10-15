// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unrecoverable_condition_on_a_held_event : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [EventType] public record AuthorSkipped(string Name);
        [EventType] public record AuthorConfirmed(string Name);
        [Command] public record RegisterAuthor(string Name)
        {
            public (object, AuthorConfirmed) Handle([GeneratedValue] AuthorId authorId)
            {
                var recorded = new AuthorRegistered(Name);
                var alias = recorded;
                return (authorId.Value == Guid.Empty ? alias : new AuthorSkipped(Name), new(Name));
            }
        }
        """));

    [Fact] void should_withhold_both_guarded_outcomes() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces AuthorRegistered", StringComparison.Ordinal) && !document.Result.Source.Contains("produces AuthorSkipped", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_production() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces AuthorConfirmed", StringComparison.Ordinal));
    [Fact] void should_report_the_conditions_at_the_held_value_use() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("condition depends", StringComparison.Ordinal)) == 2);
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
