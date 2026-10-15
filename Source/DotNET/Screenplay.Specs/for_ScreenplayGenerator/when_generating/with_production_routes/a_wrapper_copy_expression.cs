// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class a_wrapper_copy_expression : a_generated_document
{
    [Theory]
    [InlineData("new EventForEventSourceId(OtherId, new AuthorRegistered(Name)) with { EventSourceType = \"Ledger\", EventStreamType = \"Postings\" }")]
    [InlineData("(new EventForEventSourceId(OtherId, new AuthorRegistered(Name))) with { EventSourceType = \"Ledger\", EventStreamType = \"Postings\" }")]
    [InlineData("new EventForEventSourceId(OtherId, new AuthorRegistered(Name)) { EventSourceType = \"Ledger\", EventStreamType = \"Postings\" } with { EventStreamType = \"Audit\" }")]
    public void should_withhold_routes_changed_after_the_analyzed_construction(string returned)
    {
        foreach (var authoring in new[] { false, true })
        {
            foreach (var block in new[] { false, true })
            {
                var handler = block ? $"public EventForEventSourceId Handle() {{ return {returned}; }}" : $"public EventForEventSourceId Handle() => {returned};";
                Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, ProductionRouteSources.With(handler)));
                Result.Source.ShouldNotContain("stream Account.Main");
                Result.Source.ShouldNotContain("stream Ledger.Postings");
                Result.Source.ShouldNotContain("for otherId");
                Result.Source.ShouldNotContain("for id");
                Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
                Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldBeTrue();
                AssertDocument();
            }
        }
    }
}
