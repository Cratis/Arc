// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class portable_scalar_keys : a_generated_document
{
    [Theory]
    [InlineData("Guid", "Month.ToString()", "", "Uuid")]
    [InlineData("Guid", "Month.ToString(\"D\")", "", "Uuid")]
    [InlineData("StreamKey", "(string)Month", "public record StreamKey(string Value) : Cratis.Concepts.ConceptAs<string>(Value);", "StreamKey")]
    [InlineData("StreamKey", "((Guid)Month).ToString(\"D\")", "public record StreamKey(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);", "StreamKey")]
    [InlineData("StreamKey", "((int)Month).ToString(System.Globalization.CultureInfo.InvariantCulture)", "public record StreamKey(int Value) : Cratis.Concepts.ConceptAs<int>(Value);", "StreamKey")]
    [InlineData("StreamKey", "((long)Month).ToString(System.Globalization.CultureInfo.InvariantCulture)", "public record StreamKey(long Value) : Cratis.Concepts.ConceptAs<long>(Value);", "StreamKey")]
    public void should_preserve_nominal_keys_through_proven_scalar_formatting(string type, string expression, string declaration, string keyType)
    {
        foreach (var authoring in new[] { false, true })
        {
            var wrapper = ProductionRouteSources.Wrapper.Replace("EventStreamId = Month", $"EventStreamId = {expression}", StringComparison.Ordinal);
            var source = ProductionRouteSources.With($"public EventForEventSourceId Handle() => {wrapper};", properties: $"[Key] string Id, string OtherId, {type} Month, string Name") + declaration;
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldContain("stream Ledger.Postings");
            Result.Source.ShouldContain($"streamId {keyType}");
            Result.Source.ShouldContain("streamId = month");
            AssertDocument();
        }
    }

    [Theory]
    [InlineData("Month.ToString(\"N\")", "Guid")]
    [InlineData("((int)Month).ToString(System.Globalization.CultureInfo.CurrentCulture)", "StreamKey")]
    public void should_withhold_scalar_formatting_without_proven_portable_bytes(string expression, string type)
    {
        var wrapper = ProductionRouteSources.Wrapper.Replace("EventStreamId = Month", $"EventStreamId = {expression}", StringComparison.Ordinal);
        var source = ProductionRouteSources.With($"public EventForEventSourceId Handle() => {wrapper};", properties: $"[Key] string Id, string OtherId, {type} Month, string Name");
        if (type == "StreamKey")
        {
            source += "public record StreamKey(int Value) : Cratis.Concepts.ConceptAs<int>(Value);";
        }
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("stream Ledger.Postings");
        Result.Source.ShouldNotContain("for otherId");
        AssertDocument();
    }
}
