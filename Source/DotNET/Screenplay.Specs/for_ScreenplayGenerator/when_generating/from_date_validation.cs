// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_date_validation : a_generated_document
{
    [Theory]
    [InlineData("DateOnly", "GreaterThan", ">")]
    [InlineData("DateOnly", "GreaterThanOrEqualTo", ">=")]
    [InlineData("DateOnly", "LessThan", "<")]
    [InlineData("DateOnly", "LessThanOrEqualTo", "<=")]
    [InlineData("DateOnly", "Equal", "==")]
    [InlineData("DateOnly", "NotEqual", "!=")]
    [InlineData("DateOnly?", "GreaterThan", ">")]
    [InlineData("DateTimeOffset", "GreaterThan", ">")]
    [InlineData("DateTimeOffset", "GreaterThanOrEqualTo", ">=")]
    [InlineData("DateTimeOffset", "LessThan", "<")]
    [InlineData("DateTimeOffset", "LessThanOrEqualTo", "<=")]
    [InlineData("DateTimeOffset", "Equal", "==")]
    [InlineData("DateTimeOffset", "NotEqual", "!=")]
    [InlineData("DateTimeOffset?", "GreaterThan", ">")]
    public void should_bind_literal_rules_including_optional_subjects(string type, string method, string op)
    {
        var literal = type.StartsWith("DateOnly", StringComparison.Ordinal) ? "new DateOnly(2026, 1, 1)" : "DateTimeOffset.Parse(\"2026-01-01T02:00:00.1234567+02:00\", CultureInfo.InvariantCulture)";
        var expected = type.StartsWith("DateOnly", StringComparison.Ordinal) ? "2026-01-01" : "2026-01-01T00:00:00.1234567Z";
        Generate((Analyzed.SlicePath, DateComparisonSources.Validator(type, $"{method}({literal}).WithMessage(\"Date rule\")")));
        Result.Source.ShouldContain($"value {op} \"{expected}\" message \"Date rule\"");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Theory]
    [InlineData("DateOnly", "GreaterThan(value => value.Other)", false, "literal")]
    [InlineData("DateOnly", "GreaterThan(new DateOnly(2026, 1, 1))", true, "concept")]
    [InlineData("DateOnly", "GreaterThan(DateOnly.FromDateTime(DateTime.Today))", false, "clock")]
    [InlineData("DateTimeOffset", "GreaterThan(DateTimeOffset.UtcNow)", false, "clock")]
    [InlineData("DateTimeOffset", "GreaterThan(DateTimeOffset.Parse(\"2026-01-01T00:00:00\", CultureInfo.InvariantCulture))", false, "offset")]
    public void should_withhold_and_report_unadmitted_rules(string type, string rule, bool concept, string reason)
    {
        Generate((Analyzed.SlicePath, DateComparisonSources.Validator(type, rule, concept)));
        Result.Source.ShouldNotContain("value >");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Message.Contains("Value", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        AssertDocument();
    }

    [Fact]
    void should_withhold_a_literal_rule_below_v26()
    {
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V25 }, (Analyzed.SlicePath, DateComparisonSources.Validator("DateOnly", "GreaterThan(new DateOnly(2026, 1, 1))")));
        Result.Source.ShouldNotContain("value >");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("ESM v26", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
