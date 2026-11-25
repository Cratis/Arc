// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_members_have_the_same_short_name_in_another_namespace : Specification
{
    Compilation _graphs;
    Compilation _markers;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _graphs = Analyzed.Project("Library.Graphs", [], [.. given.GraphSources.Files(),
            ("Library/Other/Person.cs", "namespace Library.Other; public record Person(string Id);")]);
        _markers = given.GraphProjectionDeclarationProjects.Referencing(_graphs,
            given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.Other.Person)"));
    }

    void Because() => _result = new ScreenplayGenerator().Generate([_graphs, _markers], new ScreenplayOptions());

    [Fact] void should_compile_both_projects() => Analyzed.ErrorsIn(_graphs).Concat(Analyzed.ErrorsIn(_markers)).ShouldBeEmpty();
    [Fact] void should_not_equate_records_from_different_namespaces() => _result.Model.Graphs.Single().Projections.ShouldBeEmpty();
    [Fact] void should_identify_the_qualified_foreign_record() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("Library.Other.Person", StringComparison.Ordinal)).ShouldBeTrue();
}
