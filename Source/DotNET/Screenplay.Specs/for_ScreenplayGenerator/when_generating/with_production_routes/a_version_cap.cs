// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class a_version_cap : a_generated_document
{
    [Theory]
    [InlineData(7, false)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(10, true)]
    public void should_require_v10_for_overrides_in_both_modes(int cap, bool authoring)
    {
        var version = cap switch { 7 => SemanticVersion.V7, 8 => SemanticVersion.V8, 9 => SemanticVersion.V9, _ => SemanticVersion.V10 };
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = version, AuthoringOnlyConstructs = authoring },
            (Analyzed.SlicePath, ProductionRouteSources.With($"public EventForEventSourceId Handle() => {ProductionRouteSources.Wrapper};")));
        if (cap < 10)
        {
            Result.Source.ShouldNotContain("stream Ledger.Postings");
            Result.Source.ShouldNotContain("stream Account.Main");
            Result.Source.ShouldNotContain("for otherId");
            Result.Source.ShouldNotContain("for id");
            Result.Source.ShouldNotContain(" identifier");
            var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute && diagnostic.Message.Contains("ESM v10", StringComparison.Ordinal));
            diagnostic.Message.ShouldContain($"capped at ESM v{cap}");
            diagnostic.Location.ShouldNotBeNull();
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        }
        else
        {
            Result.Source.ShouldContain("stream Ledger.Postings");
            Result.Source.ShouldContain("for otherId");
            Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        }
        AssertDocument();
    }
}
