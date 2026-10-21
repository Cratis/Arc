// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A pin that fits the generated shape (twenty-one digits) but fails the proven Luhn rule is a server contract defect.
/// A rule stricter than the generation shape cannot be expressed with a match pattern, because the pinned Screenplay
/// prover refutes it (PLAY0688) and keeps generation in code; a checksum is the stricter rule it proves.
/// </summary>
public class from_a_checksum_violating_pin : a_checksum_scenario
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_classify_the_fixture_as_rejected_by_contract_and_withhold_the_scenario(bool authoring)
    {
        GenerateChecksumScenario(true, new() { AuthoringOnlyConstructs = authoring }, "000000000000000000001");
        Result.Source.ShouldContain("generate pattern");
        Result.Source.ShouldContain("checksum luhn");
        Result.Source.ShouldNotContain($"specification {ReceiptScenarioName}");
        Result.Source.ShouldContain($"specification {PlainScenarioName}");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Message.ShouldContain("Rejected(Contract)");
        diagnostic.Location.ShouldNotBeNull();
        AssertPartialDocument();
    }
}
