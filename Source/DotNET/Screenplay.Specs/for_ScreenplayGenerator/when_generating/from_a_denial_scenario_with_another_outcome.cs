// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario_with_another_outcome : a_generated_document
{
    [Theory]
    [InlineData("_result.ShouldNotBeAuthorized(); _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == \"Apollo\");")]
    [InlineData("_scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == \"Apollo\"); _result.ShouldNotBeAuthorized();")]
    [InlineData("_result.ShouldNotBeAuthorized(); _result.ShouldHaveValidationErrors();")]
    [InlineData("_result.ShouldHaveValidationErrors(); _result.ShouldNotBeAuthorized();")]
    public void should_omit_a_denial_combined_with_another_outcome(string assertions)
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), assertions: assertions));
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("combined with another outcome");
        AssertDocument();
    }
}
