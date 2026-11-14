// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given.RenderedQuerySources;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications;

public class with_unproven_rendered_query_scenarios : given.a_rendered_query_scenario
{
    readonly Dictionary<string, ScreenplayGenerationResult> _results = [];

    void Because()
    {
        foreach (var (name, scenario, companion) in Cases())
        {
            GenerateScenario(scenario, companion);
            AssertDocument();
            _results.Add(name, Result);
        }
    }

    [Fact] void should_reject_a_computed_argument() => AssertUnread("computed argument");
    [Fact] void should_reject_a_conditional_call() => AssertUnread("conditional call");
    [Fact] void should_reject_two_calls() => AssertUnread("multiple calls");
    [Fact] void should_reject_a_property_assertion() => AssertUnread("property assertion");
    [Fact] void should_reject_a_lookalike_the_application_does_not_declare_as_a_query() => AssertUnread("query lookalike");
    [Fact] void should_reject_text_with_a_double_quote_rather_than_alter_it() => AssertUnread("quoted text");
    [Fact] void should_reject_text_with_repeated_whitespace_rather_than_collapse_it() => AssertUnread("repeated whitespace");
    [Fact] void should_say_the_text_cannot_be_stated_exactly() => _results["quoted text"].Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("cannot state exactly", StringComparison.Ordinal));

    static IEnumerable<(string Name, string Scenario, string? Companion)> Cases()
    {
        yield return ("computed argument", Scenario
            .Replace(QueryCall, "ProjectOverview.ProjectById(_readModels, Key())", StringComparison.Ordinal)
            .Replace("\n    [Fact]", "\n    static ProjectId Key() => new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"));\n\n    [Fact]", StringComparison.Ordinal), null);
        yield return ("conditional call", Scenario.Replace(
            QueryStep,
            $"async Task Because()\n    {{\n        if (_readModels is not null)\n        {{\n            _result = await {QueryCall};\n        }}\n    }}",
            StringComparison.Ordinal), null);
        yield return ("multiple calls", Scenario.Replace(
            QueryStep,
            $"async Task Because()\n    {{\n        _result = await {QueryCall};\n        _result = await {QueryCall};\n    }}",
            StringComparison.Ordinal), null);
        yield return ("property assertion", Scenario.Replace(Assertion, "_result!.Name.ShouldEqual(_expected.Name)", StringComparison.Ordinal), null);
        yield return ("quoted text", Scenario.Replace("new ProjectName(\"Screenplay\")", "new ProjectName(\"Screen\\\"play\")", StringComparison.Ordinal), null);
        yield return ("repeated whitespace", Scenario.Replace("new ProjectName(\"Screenplay\")", "new ProjectName(\"Screen  play\")", StringComparison.Ordinal), null);
        yield return ("query lookalike", Scenario.Replace("ProjectOverview.ProjectById(_readModels", "QueryLookalike.ProjectById(_readModels", StringComparison.Ordinal), QueryLookalike);
    }

    void AssertUnread(string name)
    {
        var result = _results[name];
        result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldBeEmpty();
        result.Source.ShouldNotContain("then query");
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("when_project_by_id_is_queried", StringComparison.Ordinal));
    }
}
