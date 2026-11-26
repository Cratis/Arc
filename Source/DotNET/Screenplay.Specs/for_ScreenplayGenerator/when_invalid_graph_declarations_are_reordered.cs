// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_invalid_graph_declarations_are_reordered : given.a_generated_document
{
    ScreenplayGenerationResult _original;

    void Because()
    {
        var first = given.GraphProjectionDeclarationSources.Marker(members: "typeof(string), typeof(int)");
        var second = given.GraphProjectionDeclarationSources.Marker("Other", location: "Library.People.Other");
        Generate(given.GraphProjectionDeclarationSources.Files(first, second));
        _original = Result;
        Generate(given.GraphProjectionDeclarationSources.Files(second,
            given.GraphProjectionDeclarationSources.Marker(members: "typeof(int), typeof(string)")).Reverse().ToArray());
    }

    [Fact] void should_remain_a_compilable_partial_document() => AssertPartialDocument();
    [Fact] void should_emit_identical_text() => Result.Source.ShouldEqual(_original.Source);
    [Fact] void should_emit_identical_diagnostics() => Result.Diagnostics.ShouldEqual(_original.Diagnostics);
}
