// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_concept_date_comparisons : a_generated_document
{
    [Theory]
    [InlineData("DateOnly", "==")]
    [InlineData("DateOnly", "!=")]
    [InlineData("DateOnly", "<")]
    [InlineData("DateOnly", "<=")]
    [InlineData("DateOnly", ">")]
    [InlineData("DateOnly", ">=")]
    [InlineData("DateTime", ">=")]
    [InlineData("DateTimeOffset", ">=")]
    public void should_bind_concept_property_comparisons_at_v26(string type, string op)
    {
        var source = ConceptDateComparisonSources.Guard(type, $"End {op} Start");
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain($"when end {op} start");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Theory]
    [InlineData("DateOnly", "new DateOnly(2026, 1, 1)", "2026-01-01")]
    [InlineData("DateOnly", "DateOnly.Parse(\"2026-01-01\", CultureInfo.InvariantCulture)", "2026-01-01")]
    [InlineData("DateTime", "DateTime.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.1234567Z")]
    [InlineData("DateTimeOffset", "DateTimeOffset.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.1234567Z")]
    public void should_bind_wrapped_literals_at_v26(string type, string literal, string expected)
    {
        var source = ConceptDateComparisonSources.Guard(type, $"End >= new ReservationDate({literal})");
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain($"when end >= \"{expected}\"");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Fact]
    void should_refuse_a_constructor_that_changes_the_primitive()
    {
        var source = ConceptDateComparisonSources.Guard("DateOnly", "End >= new ReservationDate(new DateOnly(2026, 1, 1))")
            .Replace("ConceptAs<DateOnly>(Value)", "ConceptAs<DateOnly>(Value.AddDays(1))", StringComparison.Ordinal);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("when end");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("End.Value >= Start.Value", false, "direct command property")]
    [InlineData("End >= Start", true, "optional operands")]
    [InlineData("End >= new ReservationDate(DateOnly.Parse(\"not-a-date\", CultureInfo.InvariantCulture))", false, "valid calendar date")]
    public void should_refuse_unadmitted_concept_paths_and_values(string comparison, bool optional, string reason)
    {
        Generate((Analyzed.SlicePath, ConceptDateComparisonSources.Guard("DateOnly", comparison, optional)));
        Result.Source.ShouldNotContain("when end");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
