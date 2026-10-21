// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_capped_generated_checksum_scenarios : a_checksum_scenario
{
    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 20)]
    [InlineData(false, 21)]
    [InlineData(true, 21)]
    public void should_keep_the_validator_and_withhold_only_dependent_capped_scenarios(bool authoring, int cap)
    {
        GenerateChecksumScenario(true, new() { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap == 20 ? SemanticVersion.V20 : SemanticVersion.V21 });
        Result.Source.Contains("generate pattern", StringComparison.Ordinal).ShouldEqual(cap == 21);
        Result.Source.Contains($"specification {ReceiptScenarioName}", StringComparison.Ordinal).ShouldEqual(cap == 21);
        Result.Source.ShouldContain($"specification {PlainScenarioName}");
        if (cap == 20)
        {
            Result.Source.ShouldContain("rule ReceiptValidator");
            Result.Source.ShouldContain("file Feature/Slice/Slice.cs");
            Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("checksum validation requires ESM v21", StringComparison.Ordinal)).ShouldBeTrue();
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        }
        AssertPartialDocument();
        if (cap == 21 && !authoring)
        {
            AssertDocument();
            Run(ReceiptScenarioName).Passed.ShouldBeTrue();
            Run(PlainScenarioName).Passed.ShouldBeTrue();
        }
    }
}
