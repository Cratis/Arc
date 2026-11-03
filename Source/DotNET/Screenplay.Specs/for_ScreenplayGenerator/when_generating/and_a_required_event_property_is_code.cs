// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_a_required_event_property_is_code : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_the_incomplete_production_and_leave_the_handler(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, from_a_stage_shaped_command_keyed_by_its_identifier.Source(false)));
        Result.Source.ShouldNotContain("produces Recorded");
        Result.Source.ShouldContain("handler");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction &&
            diagnostic.Severity == ScreenplayDiagnosticSeverity.Information && diagnostic.Message.Contains("Recorded") && diagnostic.Message.Contains("Value")).ShouldBeTrue();
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_retain_a_production_whose_code_mapped_property_is_optional(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, from_a_stage_shaped_command_keyed_by_its_identifier.Source(false, true)));
        Result.Source.ShouldContain("produces Recorded");
        Result.Source.ShouldNotContain("value = View.value");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        AssertPartialDocument();
    }
}
