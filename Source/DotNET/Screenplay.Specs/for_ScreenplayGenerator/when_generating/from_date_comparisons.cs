// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_date_comparisons : a_generated_document
{
    [Theory]
    [InlineData("DateOnly", "==")]
    [InlineData("DateOnly", "!=")]
    [InlineData("DateOnly", "<")]
    [InlineData("DateOnly", "<=")]
    [InlineData("DateOnly", ">")]
    [InlineData("DateOnly", ">=")]
    [InlineData("DateTimeOffset", "==")]
    [InlineData("DateTimeOffset", "!=")]
    [InlineData("DateTimeOffset", "<")]
    [InlineData("DateTimeOffset", "<=")]
    [InlineData("DateTimeOffset", ">")]
    [InlineData("DateTimeOffset", ">=")]
    [InlineData("DateTime", ">=")]
    public void should_bind_and_round_trip_property_comparisons(string type, string op)
    {
        Generate((Analyzed.SlicePath, DateComparisonSources.Guard(type, $"End {op} Start")));
        Result.Source.ShouldContain($"when end {op} start");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Theory]
    [InlineData("DateOnly", "DateOnly.Parse(\"2026-01-01\", CultureInfo.InvariantCulture)", "2026-01-01")]
    [InlineData("DateOnly", "new DateOnly(2026, 1, 1)", "2026-01-01")]
    [InlineData("DateTimeOffset", "DateTimeOffset.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.1234567Z")]
    [InlineData("DateTime", "DateTime.Parse(\"2026-01-01T00:00:00Z\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.0000000Z")]
    public void should_canonicalize_literals_without_losing_ticks(string type, string literal, string expected)
    {
        Generate((Analyzed.SlicePath, DateComparisonSources.Guard(type, $"End >= {literal}")));
        Result.Source.ShouldContain($"when end >= \"{expected}\"");
        AssertDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_respect_the_version_cap_in_both_modes(bool authoring)
    {
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V25, AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, DateComparisonSources.Guard("DateOnly", "End >= Start")));
        Result.Source.ShouldNotContain("when end >= start");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("ESM v26", StringComparison.Ordinal) && diagnostic.Message.Contains("End", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
