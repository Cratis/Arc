// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class a_response_without_generated_values : a_generated_document
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Authors.Registration;
        [Command] public record EchoName(string Name)
        {
            public string Handle() => Name;
        }
        """;

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V6 }, (Analyzed.SlicePath, Source));

    [Fact] void should_withhold_the_scalar_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_keep_the_command() => Result.Source.ShouldContain("command EchoName");
    [Fact] void should_report_the_cap_with_sp0052() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("ESM v6.0");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_bind_below_v7() => SemanticVersion.V6.IsAtLeast(Bound.Value!.Model.SemanticVersion).ShouldBeTrue();
}
