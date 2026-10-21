// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_independent_response_with_unrecoverable_generation : an_unrecoverable_generated_document
{
    void Because() => GenerateAcrossOptions(WithUnavailable("""
        [Command] public record RegisterAuthor(string Name)
        {
            public (string, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId) => (Name, new(Name));
        }
        """));

    [Fact] void should_keep_the_independent_response_where_the_version_allows_it() => Documents.Where(document => document.Options.MaximumExecutableModelVersion != SemanticVersion.V6).ShouldEachConformTo(document => document.Result.Source.Contains("returns name", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_production() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("produces", StringComparison.Ordinal) && document.Result.Source.Contains("name = name", StringComparison.Ordinal));
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
