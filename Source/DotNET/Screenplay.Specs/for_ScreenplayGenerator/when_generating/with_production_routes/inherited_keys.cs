// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class inherited_keys : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_actual_inherited_key_on_a_different_definition(bool authoring)
    {
        var source = ProductionRouteSources.With("public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name)) { EventSource = typeof(LedgerEventSource), EventStream = LedgerEventSource.Postings };", "EventSourceType(\"Account\"), EventStreamType(\"Main\"), EventStreamId(\"2026-10\")");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldContain("stream Ledger.Postings");
        var route = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().Route!;
        route.StreamIdLiteral!.Value.ShouldEqual("2026-10");
        route.StreamIdType!.Name.ShouldEqual("String");
        AssertDocument();
    }

    [Theory]
    [InlineData("")]
    [InlineData(", EventStreamId = \"Default\"")]
    [InlineData(", EventStreamId = Month")]
    public void should_not_claim_an_unkeyed_override_if_its_inherited_command_key_is_unproven(string key)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = ProductionRouteSources.With($"public EventForEventSourceId Handle() => new(OtherId, new AuthorRegistered(Name)) {{ EventSourceType = \"Ledger\", EventStreamType = \"Postings\"{key} }};", "EventSourceType(\"Account\"), EventStreamType(\"Main\"), EventStreamId(\"{Id}:{Month}\")");
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            if (key.Contains("Month", StringComparison.Ordinal))
            {
                Result.Source.ShouldContain("stream Ledger.Postings");
                Result.Source.ShouldContain("streamId = month");
            }
            else
            {
                Result.Source.ShouldNotContain("stream Ledger.Postings");
                Result.Source.ShouldNotContain("for otherId");
                Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
            }
            AssertDocument();
        }
    }
}
