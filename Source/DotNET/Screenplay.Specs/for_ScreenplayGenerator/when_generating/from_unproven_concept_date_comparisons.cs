// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unproven_concept_date_comparisons : a_generated_document
{
    [Theory]
    [InlineData("new ReservationDate(2026, 1, 1)", "public ReservationDate(int year, int month, int day) : this(new DateOnly(year, month, day).AddDays(1)) { }")]
    [InlineData("ReservationDate.Parse(\"2026-01-01\", CultureInfo.InvariantCulture)", "public static ReservationDate Parse(string text, IFormatProvider provider) => new(DateOnly.Parse(text, provider).AddDays(1));")]
    public void should_refuse_concept_members_that_resemble_primitive_date_construction(string literal, string member)
    {
        var source = Source($"End > {literal}", members: member);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        AssertRefusedComparison();
    }

    [Theory]
    [InlineData("End > (ReservationDate)new DateOnly(2026, 1, 1)")]
    [InlineData("(ReservationDate)new DateOnly(2026, 1, 1) < End")]
    [InlineData("End > ((ReservationDate)new DateOnly(2026, 1, 1))")]
    public void should_refuse_an_implicit_conversion_that_shifts_the_literal(string comparison)
    {
        var source = Source(comparison, "new(value.AddDays(1))");
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        AssertRefusedComparison();
    }

    [Theory]
    [InlineData("(ReservationDate)End > new ReservationDate(new DateOnly(2026, 1, 1))")]
    [InlineData("new ReservationDate(new DateOnly(2026, 1, 1)) < (ReservationDate)End")]
    public void should_refuse_a_conversion_that_shifts_the_command_property(string comparison)
    {
        var source = Source(comparison, "new(value.AddDays(1))")
            .Replace("ReservationDate Start, ReservationDate End", "DateOnly Start, DateOnly End", StringComparison.Ordinal);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        AssertRefusedComparison();
    }

    [Theory]
    [InlineData("End > new DateOnly(2026, 1, 1)")]
    [InlineData("new DateOnly(2026, 1, 1) < End")]
    public void should_refuse_an_implicit_conversion_that_shifts_the_property_value(string comparison)
    {
        var source = Source(comparison, members: "public static implicit operator DateOnly(ReservationDate value) => value.Value.AddDays(1);");
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        AssertRefusedComparison();
    }

    [Theory]
    [InlineData("new(value)")]
    [InlineData("new ReservationDate(value)")]
    public void should_recover_an_implicit_conversion_that_forwards_the_literal(string conversion)
    {
        var source = Source("End > (ReservationDate)new DateOnly(2026, 1, 1)", conversion);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("when end > \"2026-01-01\"");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    }

    [Fact]
    void should_refuse_a_forwarding_conversion_with_a_shifting_constructor()
    {
        var source = Source("End > (ReservationDate)new DateOnly(2026, 1, 1)")
            .Replace("ConceptAs<DateOnly>(Value)", "ConceptAs<DateOnly>(Value.AddDays(1))", StringComparison.Ordinal);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        AssertRefusedComparison();
    }

    static string Source(string comparison, string conversion = "new(value)", string members = "")
    {
        var replacement = $$"""
            => {{conversion}};
            {{members}}
            """;

        return ConceptDateComparisonSources.Guard("DateOnly", comparison).Replace("=> new(value);", replacement, StringComparison.Ordinal);
    }

    void AssertRefusedComparison()
    {
        Result.Source.ShouldNotContain("when end");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).ShouldBeTrue();
        AssertPartialDocument();
    }
}
