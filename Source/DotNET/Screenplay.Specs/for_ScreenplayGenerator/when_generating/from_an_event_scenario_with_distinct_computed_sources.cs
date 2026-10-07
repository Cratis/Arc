// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_with_distinct_computed_sources : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("EventSourceId.New()", "EventSourceId.New()", "EventSourceId.New()")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_leave_the_scenario_out() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_report_the_reason() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("event sources", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
