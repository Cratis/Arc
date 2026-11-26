// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_declared_graph_projection_inputs_are_reordered : given.a_generated_document
{
    ScreenplayGenerationResult _original;

    void Because()
    {
        var first = given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.ReadModels.Graphs.Person), typeof(global::Library.ReadModels.Graphs.Company)");
        var second = given.GraphProjectionDeclarationSources.Marker("EmploymentView", "typeof(global::Library.ReadModels.Graphs.WORKS_AT)", "Library.People.Employment");
        Generate(given.GraphProjectionDeclarationSources.Files(first, second));
        _original = Result;
        Generate(given.GraphProjectionDeclarationSources.Files(second,
            given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.ReadModels.Graphs.Company), typeof(global::Library.ReadModels.Graphs.Person)")).Reverse().ToArray());
    }

    [Fact] void should_remain_compilable() => AssertDocument();
    [Fact] void should_emit_identical_text() => Result.Source.ShouldEqual(_original.Source);
    [Fact] void should_emit_identical_diagnostics() => Result.Diagnostics.ShouldEqual(_original.Diagnostics);
}
