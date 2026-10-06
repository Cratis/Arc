// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Separate allocations and separate symbols must not collapse to the same occurrence source.
/// </summary>
public class from_an_event_scenario_with_distinct_computed_sources : a_generated_document
{
    [Theory]
    [InlineData("EventSourceId.New()", "EventSourceId.New()", false)]
    [InlineData("_first", "_second", false)]
    [InlineData("_first", "_second", true)]
    public void should_leave_the_scenario_out(string given, string when, bool direct)
    {
        Generate(
            (Analyzed.SlicePath, EventAppendSources.Producer),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With(given, when, when, direct)),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("event sources", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
