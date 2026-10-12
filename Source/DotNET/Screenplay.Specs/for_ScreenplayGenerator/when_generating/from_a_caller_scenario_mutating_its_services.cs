// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Extra service-collection uses prevent proving an unchanged caller registration.
/// </summary>
public class from_a_caller_scenario_mutating_its_services : a_generated_document
{
    /// <summary>
    /// Reports service changes instead of claiming the registered caller remains installed.
    /// </summary>
    /// <param name="mutation">The competing service-collection use.</param>
    /// <param name="authorization">The command's authorization declaration.</param>
    /// <param name="code">The expected omission or retention diagnostic.</param>
    /// <param name="retained">Whether the open command scenario remains stated.</param>
    [Theory]
    [InlineData("_scenario.Services.RemoveAt(0);", "[Authorize]", "SP0039", false)]
    [InlineData("_scenario.Services.RemoveAt(0);", "", "SP0075", true)]
    [InlineData("_scenario.Services.Clear();", "[Authorize]", "SP0039", false)]
    [InlineData("_scenario.Services[0] = _scenario.Services[0];", "[Authorize]", "SP0039", false)]
    [InlineData("var services = _scenario.Services; services.Clear();", "[Authorize]", "SP0039", false)]
    [InlineData("if (_scenario.Services.Count > 0) _scenario.Services.RemoveAt(0);", "[Authorize]", "SP0039", false)]
    public void should_not_state_a_caller_whose_registration_may_be_changed(string mutation, string authorization, string code, bool retained)
    {
        Generate(CallerScenarioSources.With(CallerScenarioSources.Setup(CallerScenarioSources.RolePrincipal) + mutation, authorization));
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.ShouldNotContain("given caller");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == code).Message.ShouldContain("services");
        AssertDocument();
    }
}
