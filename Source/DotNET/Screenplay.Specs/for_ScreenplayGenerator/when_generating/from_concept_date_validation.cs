// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_concept_date_validation : a_generated_document
{
    [Theory]
    [InlineData("DateOnly", "new DateOnly(2026, 1, 1)", "2026-01-01", false)]
    [InlineData("DateOnly", "new DateOnly(2026, 1, 1)", "2026-01-01", true)]
    [InlineData("DateTime", "DateTime.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.1234567Z", false)]
    [InlineData("DateTimeOffset", "DateTimeOffset.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)", "2026-01-01T00:00:00.1234567Z", true)]
    public void should_bind_wrapped_and_primitive_literals_at_v26(string type, string literal, string expected, bool implicitConversion)
    {
        var argument = implicitConversion ? literal : $"new ReservationDate({literal})";
        var source = ConceptDateComparisonSources.Validator(type, $"GreaterThan({argument}).WithMessage(\"Date rule\")");
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain($"value > \"{expected}\" message \"Date rule\"");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Fact]
    void should_refuse_a_rule_on_a_concepts_value_path()
    {
        var source = ConceptDateComparisonSources.Validator("DateOnly", "GreaterThan(new DateOnly(2026, 1, 1))")
            .Replace("value => value.Value", "value => value.Value.Value", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("value.value >");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("DateOnly", "GreaterThan(value => value.Other)")]
    [InlineData("DateTimeOffset", "GreaterThan(DateTimeOffset.UtcNow)")]
    public void should_keep_non_literal_concept_rules_in_code(string type, string rule)
    {
        Generate((Analyzed.SlicePath, ConceptDateComparisonSources.Validator(type, rule)));
        Result.Source.ShouldNotContain("value >");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeTrue();
        AssertDocument();
    }
}
