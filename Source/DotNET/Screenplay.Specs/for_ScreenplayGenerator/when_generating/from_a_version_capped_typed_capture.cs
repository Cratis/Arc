// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_version_capped_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false, 31)]
    [InlineData(false, true, 31)]
    [InlineData(true, false, 31)]
    [InlineData(true, true, 31)]
    [InlineData(true, false, 32)]
    [InlineData(true, true, 32)]
    public void should_keep_the_whole_reactor_when_its_execution_exceeds_the_cap(bool keyIsEventSource, bool authoring, int cap)
    {
        Generate(new ScreenplayOptions
        {
            AuthoringOnlyConstructs = authoring,
            MaximumExecutableModelVersion = cap == 31 ? SemanticVersion.V31 : SemanticVersion.V32
        }, typed_capture_source.Files(keyIsEventSource));

        Result.Source.ShouldNotContain("capture OrderIntake");
        Result.Source.ShouldContain("reaction");
        Result.Source.ShouldContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ReactionMetadataVersionCap && diagnostic.Message.Contains($"requires ESM v{(keyIsEventSource ? 33 : 32)}", StringComparison.Ordinal));
        AssertDocument();
    }

    [Theory]
    [InlineData(false, false, 32)]
    [InlineData(false, true, 32)]
    [InlineData(false, false, 33)]
    [InlineData(false, true, 33)]
    [InlineData(true, false, 33)]
    [InlineData(true, true, 33)]
    public void should_keep_the_capture_when_its_execution_fits_the_cap(bool keyIsEventSource, bool authoring, int cap)
    {
        Generate(new ScreenplayOptions
        {
            AuthoringOnlyConstructs = authoring,
            MaximumExecutableModelVersion = cap == 32 ? SemanticVersion.V32 : SemanticVersion.V33
        }, typed_capture_source.Files(keyIsEventSource));

        Result.Source.ShouldContain("capture OrderIntake");
        Result.Source.ShouldNotContain("reaction");
        Result.Source.ShouldNotContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
        Result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ReactionMetadataVersionCap);
        AssertDocument();
    }
}
