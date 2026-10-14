// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_a_refusal_version_cap : a_refusing_application
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_the_whole_trigger_and_scenario_accounting_consistent_below_v19(bool authoring)
    {
        GenerateRefusal(".Acknowledge(Refused.Any)", authoring, cap: SemanticVersion.V17, sources: [(RefusalScenarioPath, RefusalScenario)]);
        AssertRefusalFallback();
        Result.Diagnostics.Single(report => report.Code == "SP0071").Message.ShouldContain("require ESM v19");
        Result.Source.ShouldContain("specification WhenRegisteringAndANameIsSupplied");
        ScenarioReports.ShouldBeEmpty();
        SemanticVersion.V17.IsAtLeast(Bound.Value!.Model.SemanticVersion).ShouldBeTrue();
    }

    [Fact]
    public void should_admit_at_v19_and_remove_scenarios_that_do_not_run_the_reaction()
    {
        GenerateRefusal(".Acknowledge(Refused.Any)", cap: SemanticVersion.V19, sources: [(RefusalScenarioPath, RefusalScenario)]);
        AssertRefusalExtraction();
        Result.Source.ShouldNotContain("specification WhenRegisteringAndANameIsSupplied");
        ScenarioReports.Single().Message.ShouldContain("sets off the declarative reaction");
    }
}
