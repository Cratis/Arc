// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Unsupported caller reporting must preserve evidence that withholds unrepresentable event sources.
/// </summary>
public class from_an_open_caller_scenario_with_unrepresentable_sources : a_generated_document
{
    /// <summary>
    /// Generates an open scenario with both an unsupported caller and an unresolved source.
    /// </summary>
    void Because()
    {
        var setup = CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", Computed())], \"test\"))") +
            " _scenario.Given.ForEventSource(Cratis.Chronicle.Events.EventSourceId.New()).Events(new AuthorRegistered(\"Prior\"));";
        Generate(CallerScenarioSources.With(setup, ""));
    }

    /// <summary>
    /// The source mismatch withholds the whole scenario.
    /// </summary>
    [Fact] void should_withhold_the_scenario() => Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");

    /// <summary>
    /// The source failure remains visible after caller filtering.
    /// </summary>
    [Fact] void should_report_the_unrepresentable_sources() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain("event sources cannot be stated faithfully");

    /// <summary>
    /// The retained document still compiles, binds, and round-trips.
    /// </summary>
    [Fact] void should_keep_the_document_valid() => AssertDocument();
}
