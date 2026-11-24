// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_declared_graph_records_are_referenced_as_metadata : Specification
{
    Compilation _graphs;
    Compilation _markers;
    ScreenplayGenerationResult _result;
    Cratis.Screenplay.Syntax.ApplicationSyntax _parsed;

    void Establish()
    {
        _graphs = given.GraphProjectionDeclarationProjects.Graphs();
        _markers = given.GraphProjectionDeclarationProjects.Referencing(_graphs, given.GraphProjectionDeclarationSources.Marker());
    }

    void Because()
    {
        _result = new ScreenplayGenerator().Generate([_graphs, _markers], new ScreenplayOptions());
        _parsed = new ScreenplayCompiler().Compile(_result.Source).Value!;
    }

    [Fact] void should_compile_the_graph_project() => Analyzed.ErrorsIn(_graphs).ShouldBeEmpty();
    [Fact] void should_compile_the_marker_project() => Analyzed.ErrorsIn(_markers).ShouldBeEmpty();
    [Fact] void should_resolve_metadata_members_to_the_source_graph() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration).ShouldBeEmpty();
    [Fact] void should_keep_the_named_projection_in_its_declaring_slice() => _parsed.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "Directory").GraphProjections.Single().Name.ShouldEqual("PeopleDirectory");
    [Fact] void should_keep_all_owned_records() => _result.Model.Graphs.Single().Projections.Single().Elements.Count.ShouldEqual(2);
}
