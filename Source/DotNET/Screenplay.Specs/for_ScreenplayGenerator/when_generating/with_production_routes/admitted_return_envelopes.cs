// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class admitted_return_envelopes : a_generated_document
{
    [Theory]
    [InlineData("public EventForEventSourceId Handle() => WRAPPER;")]
    [InlineData("public EventForEventSourceId Handle() { return WRAPPER; }")]
    [InlineData("public Task<EventForEventSourceId> Handle() => Task.FromResult(WRAPPER);")]
    [InlineData("public Cratis.Monads.Result<EventForEventSourceId, string> Handle() => WRAPPER;")]
    [InlineData("public Cratis.Monads.Result<(AuthorAudited, EventForEventSourceId), string> Handle() => (new AuthorAudited(Name), WRAPPER);")]
    [InlineData("public ValueTask<EventForEventSourceId> Handle() => new(WRAPPER);")]
    [InlineData("public ValueTask<EventForEventSourceId> Handle() => ValueTask.FromResult(WRAPPER);")]
    [InlineData("public async Task<EventForEventSourceId> Handle() { await Task.CompletedTask; return WRAPPER; }")]
    [InlineData("public EventForEventSourceId[] Handle() => [WRAPPER];")]
    [InlineData("public EventForEventSourceId[] Handle() => new[] { WRAPPER };")]
    [InlineData("public List<EventForEventSourceId> Handle() => new() { WRAPPER };")]
    [InlineData("public List<EventForEventSourceId> Handle() => new List<EventForEventSourceId> { WRAPPER };")]
    [InlineData("public (AuthorAudited, EventForEventSourceId) Handle() => (new(Name), WRAPPER);")]
    [InlineData("public (AuthorAudited, EventForEventSourceId[]) Handle() => (new(Name), [WRAPPER]);")]
    [InlineData("public (AuthorAudited, IEnumerable<EventForEventSourceId>) Handle() => (new(Name), [WRAPPER]);")]
    [InlineData("public (AuthorAudited, List<EventForEventSourceId>) Handle() => (new(Name), new() { WRAPPER });")]
    [InlineData("public (AuthorAudited, List<EventForEventSourceId>) Handle() => (new(Name), new List<EventForEventSourceId> { WRAPPER });")]
    [InlineData("public object[] Handle() => [new AuthorAudited(Name), WRAPPER];")]
    [InlineData("public List<object> Handle() => new() { new AuthorAudited(Name), WRAPPER };")]
    public void should_emit_the_effective_destination_and_route_through_supported_returns(string handler)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
                (Analyzed.SlicePath, ProductionRouteSources.With(handler.Replace("WRAPPER", ProductionRouteSources.Wrapper, StringComparison.Ordinal))));
            var production = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single(production => production.EventName == "AuthorRegistered");
            production.Destination.ShouldEqual(new PropertyPathSource("OtherId"));
            production.Route!.Source.ShouldEqual("Ledger");
            production.Route.Stream.ShouldEqual("Postings");
            production.Route.StreamId.ShouldEqual("Month");
            Result.Source.ShouldContain("for otherId");
            Result.Source.ShouldContain("stream Ledger.Postings");
            Result.Source.ShouldContain("streamId = month");
            if (handler.Contains("AuthorAudited", StringComparison.Ordinal))
            {
                Result.Source.ShouldContain("for id");
            }
            Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeEmpty();
            AssertDocument();
            Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        }
    }
}
