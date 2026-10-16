// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class date_comparison_operands : Specification
{
    [Theory]
    [InlineData("DateOnly", "End >= Start", "Start")]
    [InlineData("DateTimeOffset", "End >= Start", "Start")]
    [InlineData("DateTime", "End >= Start", "Start")]
    public void should_recover_both_sides_of_a_property_comparison(string type, string comparison, string right)
    {
        var source = DateComparisonSources.Guard(type, comparison);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        var analysis = Analyzed.Source(source);
        var production = analysis.Slice().Commands.Single().Produces.First();
        production.When.ShouldEqual(new ComparisonCondition("End", ComparisonKind.GreaterThanOrEqual, new PropertyPathSource(right)));
    }

    [Fact]
    void should_recover_a_canonical_literal_on_the_left_with_a_mirrored_operator()
    {
        var source = DateComparisonSources.Guard("DateOnly", "new DateOnly(2026, 1, 1) < End");
        var analysis = Analyzed.Source(source);
        analysis.Slice().Commands.Single().Produces.First().When.ShouldEqual(new ComparisonCondition("End", ComparisonKind.GreaterThan, new LiteralSource("2026-01-01")));
    }
}
