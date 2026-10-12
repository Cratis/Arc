// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Earlier provider initialization prevents claiming that a later registration installed the caller.
/// </summary>
public class from_a_caller_scenario_initialized_before_establish : a_generated_document
{
    /// <summary>
    /// Constructors and setup helpers cannot initialize the scenario before caller registration.
    /// </summary>
    /// <param name="member">The source member containing the earlier initialization.</param>
    /// <param name="establish">The optional setup call reaching that member.</param>
    [Theory]
    [InlineData("public with_a_caller() { _scenario.Validate(new RegisterAuthor(\"author\", \"Prior\")); }", "")]
    [InlineData("void InitializeScenario() => _scenario.Validate(new RegisterAuthor(\"author\", \"Prior\"));", "InitializeScenario();")]
    public void should_withhold_an_authorized_scenario_initialized_before_caller_registration(string member, string establish)
    {
        var sources = CallerScenarioSources.With(establish + CallerScenarioSources.Setup(CallerScenarioSources.RolePrincipal));
        Generate(sources.Select(source => (source.Path, source.Text.Replace("void Establish()", member + " void Establish()", StringComparison.Ordinal))).ToArray());
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain("initialized before the caller is registered");
        AssertDocument();
    }
}
