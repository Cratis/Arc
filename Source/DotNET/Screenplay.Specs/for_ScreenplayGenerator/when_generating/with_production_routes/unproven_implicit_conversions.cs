// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class unproven_implicit_conversions : a_generated_document
{
    [Theory]
    [InlineData("Destination OtherId, string Month", "EventForEventSourceId", "public record Destination(string Value) : Cratis.Concepts.ConceptAs<string>(Value) { public static implicit operator EventSourceId(Destination value) => new(\"changed-\" + value.Value); }")]
    [InlineData("string OtherId, StreamKey Month", "EventForEventSourceId", "public record StreamKey(string Value) : Cratis.Concepts.ConceptAs<string>(Value) { public static implicit operator EventStreamId(StreamKey value) => new(\"changed-\" + value.Value); }")]
    [InlineData("string OtherId, string Month", "WrappedResponse", "public record WrappedResponse(EventForEventSourceId Value) { public static implicit operator WrappedResponse(EventForEventSourceId value) => new(value); }")]
    public void should_not_peel_user_conversions_that_change_the_destination_key_or_return_effect(string inputs, string result, string declaration)
    {
        foreach (var authoring in new[] { false, true })
        {
            var source = ProductionRouteSources.With($"public {result} Handle() => {ProductionRouteSources.Wrapper};", properties: $"[Key] string Id, {inputs}, string Name") + declaration;
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldNotContain("for otherId");
            Result.Source.ShouldNotContain("stream Ledger.Postings");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
            AssertDocument();
        }
    }
}
