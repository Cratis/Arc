// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class withheld_routes : a_generated_document
{
    [Theory]
    [InlineData("new(OtherId + \"-computed\", new AuthorRegistered(Name))")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventStreamId = Month + \"-computed\" }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventStreamType = Month }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventSource = typeof(LedgerEventSource) }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventSource = typeof(LedgerEventSource), EventStream = \"Unknown\" }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { Subject = \"someone\" }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { Tags = [\"tag\"] }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { Occurred = DateTimeOffset.UtcNow }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventStreamId = \"\" }")]
    [InlineData("new(OtherId, new AuthorRegistered(Name)) { EventStreamId = \"e\\u0301\" }")]
    public void should_not_turn_unknown_routes_or_metadata_into_command_context(string wrapper)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, ProductionRouteSources.With($"public EventForEventSourceId Handle() => {wrapper};")));
            Result.Source.ShouldNotContain("for otherId");
            Result.Source.ShouldNotContain("for id");
            Result.Source.ShouldNotContain("stream Account.Main");
            Result.Source.ShouldNotContain(" identifier");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
            AssertDocument();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_all_route_claims_without_orphan_declarations_if_a_sibling_destination_is_unknown(bool authoring)
    {
        var source = ProductionRouteSources.With($$"""
            public EventForEventSourceId[] Handle() => [
                {{ProductionRouteSources.Wrapper}},
                new(OtherId + "-computed", new AuthorRegistered(Name))
            ];
            """);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("eventsource Ledger");
        Result.Source.ShouldNotContain("stream Ledger.Postings");
        Result.Source.ShouldNotContain("stream Account.Main");
        Result.Source.ShouldNotContain("for otherId");
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
        AssertDocument();
    }

    [Theory]
    [InlineData("public EventForEventSourceId Handle() { var unused = WRAPPER; return new(OtherId, new AuthorRegistered(Name)); }")]
    [InlineData("public object[] Handle() => [.. Build(), WRAPPER]; public object[] Build() => []; ")]
    [InlineData("public object[] Handle() => [Build(), WRAPPER]; public AuthorAudited Build() => new(Name);")]
    [InlineData("public List<EventForEventSourceId> Handle() => new(Build()) { WRAPPER }; public EventForEventSourceId[] Build() => [];")]
    public void should_not_use_unreturned_wrappers_or_collection_spreads_as_route_proof(string handler)
    {
        Generate((Analyzed.SlicePath, ProductionRouteSources.With(handler.Replace("WRAPPER", ProductionRouteSources.Wrapper, StringComparison.Ordinal), string.Empty)));
        Result.Source.ShouldNotContain("stream Ledger.Postings");
        Result.Source.ShouldNotContain("for otherId");
        AssertDocument();
    }
}
