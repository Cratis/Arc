// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_version_capped_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false, 32)]
    [InlineData(false, true, 32)]
    [InlineData(true, false, 32)]
    [InlineData(true, true, 32)]
    [InlineData(false, false, 40)]
    [InlineData(false, true, 40)]
    [InlineData(true, false, 40)]
    [InlineData(true, true, 40)]
    public void should_keep_the_whole_reactor_when_its_execution_exceeds_the_cap(bool keyIsEventSource, bool authoring, int cap)
    {
        Generate(new ScreenplayOptions
        {
            AuthoringOnlyConstructs = authoring,
            MaximumExecutableModelVersion = cap == 32 ? SemanticVersion.V32 : SemanticVersion.V40
        }, typed_capture_source.Files(keyIsEventSource));

        Result.Source.ShouldNotContain("capture OrderIntake");
        Result.Source.ShouldContain("reaction");
        Result.Source.ShouldContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ReactionMetadataVersionCap && diagnostic.Message.Contains("ESM v41", StringComparison.Ordinal));
        AssertDocument();
    }

    [Theory]
    [InlineData(false, false, 41)]
    [InlineData(false, true, 41)]
    [InlineData(true, false, 41)]
    [InlineData(true, true, 41)]
    [InlineData(false, false, 42)]
    [InlineData(false, true, 42)]
    [InlineData(true, false, 42)]
    [InlineData(true, true, 42)]
    public void should_keep_the_capture_when_its_execution_fits_the_cap(bool keyIsEventSource, bool authoring, int cap)
    {
        Generate(new ScreenplayOptions
        {
            AuthoringOnlyConstructs = authoring,
            MaximumExecutableModelVersion = cap == 41 ? SemanticVersion.V41 : SemanticVersion.V42
        }, typed_capture_source.Files(keyIsEventSource));

        Result.Source.ShouldContain("capture OrderIntake");
        Result.Source.ShouldNotContain("reaction");
        Result.Source.ShouldNotContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
        Result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ReactionMetadataVersionCap);
        AssertDocument();
    }
}
