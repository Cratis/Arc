// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class nominal_destinations : a_generated_document
{
    [Theory]
    [InlineData("EventSourceId", "OtherId", "")]
    [InlineData("EventSourceId<Guid>", "OtherId", "")]
    [InlineData("LedgerId", "OtherId", "public record LedgerId(Guid Value) : EventSourceId<Guid>(Value);")]
    [InlineData("string", "new EventSourceId(OtherId)", "")]
    [InlineData("LedgerId", "new EventSourceId((string)OtherId)", "[Cratis.Chronicle.Compliance.GDPR.PII] public record LedgerId(string Value) : Cratis.Concepts.ConceptAs<string>(Value);")]
    public void should_recover_typed_and_untyped_source_ids_without_erasing_the_input_type(string type, string expression, string declaration)
    {
        foreach (var authoring in new[] { false, true })
        {
            var wrapper = ProductionRouteSources.Wrapper.Replace("(OtherId,", $"({expression},", StringComparison.Ordinal);
            var source = ProductionRouteSources.With($"public EventForEventSourceId Handle() => {wrapper};", properties: $"[Key] string Id, {type} OtherId, string Month, string Name") + declaration;
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            Result.Source.ShouldContain("for otherId");
            Result.Source.ShouldContain("stream Ledger.Postings");
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Produces.Single().Route!.IdentifierType.ShouldEqual(command.Properties.Single(property => property.Name == "OtherId").Type);
            if (declaration.Contains("PII", StringComparison.Ordinal))
            {
                Result.Source.ShouldNotContain("  pii");
                Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation).ShouldBeTrue();
            }
            AssertDocument();
        }
    }
}
