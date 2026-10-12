// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_parameterized_theory_scenario : a_generated_document
{
    void Because()
    {
        var sources = CallerScenarioSources.With("", "");
        Generate(sources.Select(source => (source.Path, source.Text.Replace(
            "async Task Because() => await _scenario.Execute(new RegisterAuthor(\"author\", \"Apollo\"));\n    [Fact] Task should_register() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == \"Apollo\");",
            "[Theory]\n    [InlineData(\"Apollo\")]\n    async Task should_register(string name) { await _scenario.Execute(new RegisterAuthor(\"author\", name)); await _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == name); }",
            StringComparison.Ordinal))).ToArray());
    }

    [Theory]
    [InlineData("_scenario.Execute(new RegisterAuthor(\"author\", \"Apollo\"))", true)]
    [InlineData("_client.ExecuteCommand(\"/register\", new RegisterAuthor(\"author\", \"Apollo\"))", false)]
    public void should_report_theories_without_losing_ordinary_facts(string action, bool keepFact)
    {
        var sources = CallerScenarioSources.With("", "", action: action);
        const string Theory = "[Theory] [InlineData(\"Apollo\")] Task should_theory_register(string name) => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == name);";
        const string Assertion = "[Fact] Task should_register() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == \"Apollo\");";
        var replacement = keepFact ? $"{Theory} {Assertion}" : Theory;
        Generate(sources.Select(source => (source.Path, source.Text.Replace(Assertion, replacement, StringComparison.Ordinal))).ToArray());
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076").Message.ShouldContain("should_theory_register");
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(keepFact);
        AssertDocument();
    }

    [Fact] void should_report_the_parameterized_scenario() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_name_the_omitted_theory() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076").Message.ShouldContain("with_a_caller.should_register");
    [Fact] void should_not_state_the_scenario() => Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
    [Fact] void should_keep_the_document_valid() => AssertDocument();
}
