// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class an_unavailable_converted_tuple_identity : an_unrecoverable_generated_document
{
    [Theory]
    [InlineData("Guid")]
    [InlineData("string")]
    public void should_withhold_an_unavailable_identity_after_the_contextual_conversion(string type)
    {
        GenerateAcrossOptions(WithUnavailable($$"""
            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor([Key] string Id, string Name)
            {
                public (EventSourceId, AuthorRegistered) Handle([GeneratedValue] {{type}} destination)
                    => (destination, new(Name));
            }
            """));
        Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("produces", StringComparison.Ordinal));
        Documents.ShouldEachConformTo(document => document.Result.Source.Contains("handler", StringComparison.Ordinal));
        Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult && diagnostic.Message.Contains("explicit destination depends", StringComparison.Ordinal)));
        AssertFaithfulDocuments();
    }
}
