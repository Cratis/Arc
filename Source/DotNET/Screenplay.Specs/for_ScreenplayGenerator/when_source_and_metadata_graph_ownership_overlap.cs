// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_source_and_metadata_graph_ownership_overlap : Specification
{
    Compilation _graphs;
    Compilation _markers;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _graphs = given.GraphProjectionDeclarationProjects.Graphs(given.GraphProjectionDeclarationSources.Marker());
        _markers = given.GraphProjectionDeclarationProjects.Referencing(_graphs,
            given.GraphProjectionDeclarationSources.Marker("Other", location: "Library.People.Other", type: "OtherMarker"));
    }

    void Because() => _result = new ScreenplayGenerator().Generate([_graphs, _markers], new ScreenplayOptions());

    [Fact] void should_compile_both_projects() => Analyzed.ErrorsIn(_graphs).Concat(Analyzed.ErrorsIn(_markers)).ShouldBeEmpty();
    [Fact] void should_withhold_the_entire_projection_set() => _result.Model.Graphs.Single().Projections.ShouldBeEmpty();
    [Fact] void should_detect_the_shared_identity_as_duplicate_ownership() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("already owned", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_misdiagnose_metadata_as_a_foreign_record() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("member must be an annotated", StringComparison.Ordinal)).ShouldBeFalse();
}
