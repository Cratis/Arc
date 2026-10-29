// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class tuple_destinations : a_production_route_document
{
    [Theory]
    [InlineData("public (string, EventForEventSourceId) Handle() => (Name, WRAPPER);")]
    [InlineData("public (EventForEventSourceId, string) Handle() => (WRAPPER, Name);")]
    [InlineData("public (Receipt, EventForEventSourceId) Handle() => (new Receipt(Name), WRAPPER);")]
    [InlineData("public (EventForEventSourceId, Receipt) Handle() => (WRAPPER, new(Name));")]
    [InlineData("public (string, EventForEventSourceId[]) Handle() => (Name, [WRAPPER]);")]
    [InlineData("public (EventForEventSourceId[], string) Handle() => ([WRAPPER], Name);")]
    [InlineData("public (string, List<EventForEventSourceId>) Handle() => (Name, new() { WRAPPER });")]
    [InlineData("public (List<EventForEventSourceId>, string) Handle() => (new List<EventForEventSourceId> { WRAPPER }, Name);")]
    [InlineData("public (string, object[]) Handle() => (Name, [WRAPPER, new AuthorAudited(Name)]);")]
    [InlineData("public (object[], string) Handle() => (new object[] { WRAPPER, new AuthorAudited(Name) }, Name);")]
    [InlineData("public Task<(string, EventForEventSourceId)> Handle() => Task.FromResult((Name, WRAPPER));")]
    [InlineData("public Cratis.Monads.Result<(string, EventForEventSourceId), string> Handle() => (Name, WRAPPER);")]
    public void should_preserve_the_response_and_each_inline_destination(string handler)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = TupleDestinationSources.With(handler.Replace("WRAPPER", TupleDestinationSources.Wrapper, StringComparison.Ordinal));
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Produces.Single(production => production.EventName == "AuthorRegistered").Destination.ShouldEqual(new PropertyPathSource("OtherId"));
            command.Authoring!.ResponseType.ShouldNotBeNull();
            Result.Source.ShouldContain("for otherId");
            Result.Source.ShouldContain("returns");
            if (handler.Contains("AuthorAudited", StringComparison.Ordinal))
            {
                command.Produces.Single(production => production.EventName == "AuthorAudited").UsesCommandContext.ShouldBeTrue();
                Result.Source.ShouldContain("for id");
            }
            Result.Diagnostics.ShouldBeEmpty();
            AssertRoutedDocument();
            var boundCommand = Bound.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
            var destination = boundCommand.Produces[0].Destination as SemanticResolvedExpression;
            destination.ShouldNotBeNull();
            destination!.Target.ShouldEqual(boundCommand.Properties.Single(property => property.Name == "otherId").Id);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_independent_routes_for_each_tuple_element(bool authoring)
    {
        var source = TupleDestinationSources.With($"public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, {TupleDestinationSources.Wrapper}, {TupleDestinationSources.RoutedWrapper});");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        var productions = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.ToList();
        productions.Count.ShouldEqual(2);
        productions[0].Destination.ShouldEqual(new PropertyPathSource("OtherId"));
        productions[1].Destination.ShouldEqual(new PropertyPathSource("ThirdId"));
        productions[1].Route!.Source.ShouldEqual("Ledger");
        productions[1].Route!.Stream.ShouldEqual("Postings");
        productions[1].Route!.StreamId.ShouldEqual("Month");
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldContain("for thirdId");
        Result.Source.ShouldContain("returns name");
        Result.Diagnostics.ShouldBeEmpty();
        AssertRoutedDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    }
}
