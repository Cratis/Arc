// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class distinct_occurrences : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_the_same_event_on_different_routes_in_return_order(bool authoring)
    {
        var source = ProductionRouteSources.With($$"""
                public object[] Handle() => [
                    new AuthorRegistered(Name),
                    {{ProductionRouteSources.Wrapper}},
                    new EventForEventSourceId(OtherId, new AuthorRegistered(Name)) { EventSourceType = "Ledger", EventStreamType = "Audit" }
                ];
                """);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        var productions = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.ToList();
        productions.Count.ShouldEqual(3);
        productions[0].UsesCommandContext.ShouldBeTrue();
        productions[1].Destination.ShouldEqual(new PropertyPathSource("OtherId"));
        productions[1].Route!.Stream.ShouldEqual("Postings");
        productions[2].Route!.Stream.ShouldEqual("Audit");
        Result.Source.ShouldContain("for id");
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldContain("stream Account.Main");
        Result.Source.ShouldContain("stream Ledger.Postings");
        Result.Source.ShouldContain("stream Ledger.Audit");
        AssertDocument();
    }

    [Theory]
    [InlineData("EventSourceId")]
    [InlineData("EventSourceId<Guid>")]
    public void should_recover_a_typed_identity_tuple_without_treating_a_response_as_an_identity(string identity)
    {
        Generate((Analyzed.SlicePath, ProductionRouteSources.With(
            $"public ({identity}, AuthorRegistered) Handle() => (OtherId, new(Name));",
            properties: $"{identity} OtherId, string Month, string Name")));
        Result.Source.ShouldContain("for otherId");
        Result.Source.ShouldContain("stream Account.Main");
        AssertDocument();
    }

    [Fact]
    void should_keep_a_plain_event_response_tuple_in_command_context()
    {
        Generate((Analyzed.SlicePath, ProductionRouteSources.With("public (AuthorRegistered, string) Handle() => (new(Name), OtherId);")));
        Result.Source.ShouldContain("for id");
        Result.Source.ShouldNotContain("for otherId");
        AssertDocument();
    }
}
