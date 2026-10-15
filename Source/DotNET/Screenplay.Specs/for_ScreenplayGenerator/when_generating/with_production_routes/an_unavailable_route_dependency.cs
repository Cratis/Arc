// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class an_unavailable_route_dependency : an_unrecoverable_generated_document
{
    [Theory]
    [InlineData("EventStreamId = authorId.ToString()")]
    [InlineData("EventStreamType = authorId.ToString()")]
    [InlineData("EventSourceType = authorId.ToString()")]
    public void should_drop_the_whole_dependent_production_in_every_mode_and_cap(string assignment)
    {
        GenerateAcrossOptions(WithUnavailable($$"""
            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor([Key] string Id, string OtherId, string Name)
            {
                public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue] AuthorId authorId)
                    => new(Event: new AuthorRegistered(Name), EventSourceId: OtherId) { {{assignment}} };
            }
            """));
        Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
        Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
        Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("stream Account.Transactions", StringComparison.Ordinal));
        Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
        AssertFaithfulDocuments();
    }
}
