// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_response_only_command : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Authors.Registration;
        [Command] public record EchoName(string Name)
        {
            public string Handle() => Name;
        }
        """));

    [Fact] void should_return_the_property_by_default() => Result.Source.ShouldContain("returns name");
    [Fact] void should_not_emit_an_unadmitted_handler() => Result.Source.ShouldNotContain("handler");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_select_v7_for_returns_without_generation() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
}
