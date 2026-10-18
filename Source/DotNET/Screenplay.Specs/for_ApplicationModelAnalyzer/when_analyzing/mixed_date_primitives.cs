// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class mixed_date_primitives : Specification
{
    [Fact]
    void should_refuse_a_concept_against_a_different_date_primitive()
    {
        var analysis = Analyzed.Source(ConceptDateComparisonSources.Guard("DateOnly", "End >= DateTime.UtcNow"));
        analysis.Slice().Commands.Single().Produces.First().When.ShouldBeNull();
        analysis.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("cannot be mixed", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_report_the_property_and_not_emit_the_mixed_comparison()
    {
        // C# also refuses this operator. Partial-source analysis must not turn it into a bindable-looking mixed comparison.
        var analysis = Analyzed.Source(DateComparisonSources.Guard("DateOnly", "End >= DateTime.UtcNow"));
        analysis.Slice().Commands.Single().Produces.First().When.ShouldBeNull();
        analysis.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("End", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("cannot be mixed", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
