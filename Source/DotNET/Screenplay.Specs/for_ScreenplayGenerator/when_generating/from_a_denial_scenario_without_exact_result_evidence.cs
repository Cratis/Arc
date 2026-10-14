// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario_without_exact_result_evidence : a_generated_document
{
    [Theory]
    [InlineData("new Cratis.Arc.Commands.CommandResult().ShouldNotBeAuthorized();", "stable result")]
    [InlineData("_result = new(); _result.ShouldNotBeAuthorized();", "stable result")]
    [InlineData("_result.ShouldNotBeAuthorized(); new Cratis.Arc.Commands.CommandResult().ShouldNotBeSuccessful();", "stable result")]
    [InlineData("if (_result.IsAuthorized) _result.ShouldNotBeAuthorized();", "conditional")]
    public void should_omit_an_unproven_denial_result(string assertions, string reason)
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), assertions: assertions));
        AssertOmitted(reason);
    }

    [Fact]
    public void should_omit_an_unrelated_helper_with_the_denial_name()
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"))
            .Select(source => (source.Path, source.Text.Replace("class CommandResultShouldExtensions", "class UnrelatedAssertions", StringComparison.Ordinal))).ToArray());
        AssertOmitted("exact command-result testing API");
    }

    void AssertOmitted(string reason)
    {
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain(reason);
        AssertDocument();
    }
}
