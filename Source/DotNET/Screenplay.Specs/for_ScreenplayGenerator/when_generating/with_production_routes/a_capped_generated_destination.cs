// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class a_capped_generated_destination : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_prune_the_whole_override_if_its_generated_destination_is_capped(bool authoring, bool capped)
    {
        var source = GeneratedValueSources.With("""
            [Command, EventSourceType("Account"), EventStreamType("Main")]
            public record RegisterAuthor(string Month, string Name)
            {
                public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue] AuthorId authorId)
                    => new(authorId, new AuthorRegistered(Name)) { EventSourceType = "Ledger", EventStreamType = "Postings", EventStreamId = Month };
            }
            """);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = capped ? SemanticVersion.V6 : SemanticVersion.V10 }, (Analyzed.SlicePath, source));
        if (capped)
        {
            Result.Source.ShouldNotContain("produces");
            Result.Source.ShouldNotContain("for authorId");
            Result.Source.ShouldNotContain("stream Ledger.Postings");
            Result.Source.ShouldContain("handler");
            Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("explicit destination or route reads an unadmitted generated value", StringComparison.Ordinal)).ShouldBeTrue();
            AssertPartialDocument();
        }
        else
        {
            Result.Source.ShouldContain("for authorId");
            Result.Source.ShouldContain("stream Ledger.Postings");
            AssertDocument();
        }
    }
}
