// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_members_have_the_same_name_in_another_assembly : Specification
{
    Compilation _graphs;
    Compilation _markers;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _graphs = given.GraphProjectionDeclarationProjects.Graphs();
        const string source = """
            [assembly: System.Reflection.AssemblyVersion("2.0.0.0")]
            namespace Library.ReadModels.Graphs;
            public record Person(string Id);
            """;
        var foreign = Analyzed.Project("Library.Graphs", [], ("Foreign/Person.cs", source));
        _markers = given.GraphProjectionDeclarationProjects.Referencing(foreign,
            given.GraphProjectionDeclarationSources.Marker(members: "typeof(global::Library.ReadModels.Graphs.Person)"));
    }

    void Because() => _result = new ScreenplayGenerator().Generate([_graphs, _markers], new ScreenplayOptions());

    [Fact] void should_compile_both_projects() => Analyzed.ErrorsIn(_graphs).Concat(Analyzed.ErrorsIn(_markers)).ShouldBeEmpty();
    [Fact] void should_not_equate_records_from_different_assemblies() => _result.Model.Graphs.Single().Projections.ShouldBeEmpty();
    [Fact] void should_diagnose_the_foreign_record() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration && diagnostic.Message.Contains("member must be an annotated", StringComparison.Ordinal)).ShouldBeTrue();
}
