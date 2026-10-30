// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class withheld_tuple_destinations : a_generated_document
{
    [Theory]
    [InlineData("public (string, EventForEventSourceId) Handle() { var wrapped = WRAPPER; return (Name, wrapped); }")]
    [InlineData("public (string, object[]) Handle() => (Name, [.. Build(), WRAPPER]); public object[] Build() => [];")]
    [InlineData("public (string, object[]) Handle() => (Name, [Build(), WRAPPER]); public AuthorAudited Build() => new(Name);")]
    [InlineData("public (string, (EventForEventSourceId, AuthorAudited)) Handle() => (Name, (WRAPPER, new(Name)));")]
    [InlineData("public (string, string, EventForEventSourceId) Handle() => (Name, Month, WRAPPER);")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId + \"computed\", new AuthorAudited(Name)));")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Subject = \"someone\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Tags = [\"tag\"] });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Occurred = DateTimeOffset.UtcNow });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventStreamId = Month + \"computed\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventSource = typeof(LedgerEventSource), EventStream = \"Unknown\" });")]
    [InlineData("public (Receipt, EventForEventSourceId) Handle() => (Converted, WRAPPER); public ConvertedReceipt Converted => new(Name);")]
    public void should_withhold_all_tuple_routes_instead_of_falling_back_to_command_context(string handler)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = TupleDestinationSources.With(handler.Replace("WRAPPER", TupleDestinationSources.RoutedWrapper, StringComparison.Ordinal));
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldNotContain("for otherId");
            Result.Source.ShouldNotContain("for thirdId");
            Result.Source.ShouldNotContain("for id");
            Result.Source.ShouldNotContain("eventsource Ledger");
            Result.Source.ShouldNotContain("stream Account.Main");
            Result.Source.ShouldNotContain("otherId String identifier");
            Result.Source.ShouldNotContain("thirdId String identifier");
            Result.Source.ShouldNotContain(" generated identifier");
            Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
            AssertDocument();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_not_discover_tuple_effects_in_a_helper_method(bool authoring)
    {
        var source = TupleDestinationSources.With($"public (string, EventForEventSourceId) Handle() => (Name, Build()); public EventForEventSourceId Build() => {TupleDestinationSources.RoutedWrapper};");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.ShouldBeEmpty();
        Result.Source.ShouldNotContain("eventsource Ledger");
        Result.Source.ShouldNotContain("for thirdId");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeEmpty();
        AssertPartialDocument();
    }
}
